using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.Common;

namespace GitHub.Services.Content.Common.Telemetry
{
    public abstract class ClientTelemetry : IDisposable
    {
        // Retained for Drop client compatibility, use ActionResult going forward.
        public const string SuccessfulActionResult = "OK";
        public const string UnknownActionResult = "Unknown";
        public enum ActionResult
        {
            Unknown,
            Success,
            Failure
        }

        protected internal TelemetryInformationLevel Level;
        protected internal readonly Uri VstsUri;
        protected readonly IAppTraceSource tracer;
        protected readonly bool enabled;
        protected readonly IList<ITelemetrySender> senders;

        public ClientTelemetry(TelemetryInformationLevel level, Uri vstsUri, IAppTraceSource tracer, bool enable, IEnumerable<Func<ITelemetrySender>> senderFactories)
        {
            ArgumentUtility.CheckForNull(tracer, nameof(tracer));

            this.Level = level;
            this.VstsUri = vstsUri;
            this.tracer = tracer;
            this.enabled = enable;

            this.senders = new List<ITelemetrySender>();
            if (enable && senderFactories != null)
            {
                foreach (var senderFactory in senderFactories)
                {
                    ITelemetrySender sender = null;

                    try
                    {
                        sender = senderFactory();
                    }
                    catch (Exception e)
                    {
                        this.tracer.Warn(e, $"Ignored failure of Func<{nameof(ITelemetrySender)}> {nameof(senderFactory)}.");
                    }

                    if (sender != null)
                    {
                        try
                        {
                            sender.StartSender();
                            this.senders.Add(sender);
                            this.tracer.Verbose($"Started {nameof(ITelemetrySender)} {sender.GetType().Name}.");
                        }
                        catch (Exception e)
                        {
                            this.tracer.Warn(e, $"Omitted {nameof(ITelemetrySender)} {sender.GetType().Name} because it failed during {nameof(sender.StartSender)}.");
                        }
                    }
                }
            }
        }

        public virtual ActionTelemetryRecord CreateAction(string actionName, uint attemptCount = 1)
        {
            return new ActionTelemetryRecord(this.Level, this.VstsUri, actionName, attemptCount);
        }

        public T CreateRecord<T>(Func<TelemetryInformationLevel, Uri, T> factory) where T : TelemetryRecord
        {
            return factory(this.Level, this.VstsUri);
        }

        protected void ExecuteOnAllSendersWithExceptionLogging(Action<ITelemetrySender> action)
        {
            if (this.enabled)
            {
                foreach (var sender in this.senders)
                {
                    try
                    {
                        action(sender);
                    }
                    catch (Exception e)
                    {
                        this.tracer.Info($"Exception during {nameof(ExecuteOnAllSendersWithExceptionLogging)} for {sender.GetType().Name}.\r\n{e.ToString()}");
                    }
                }
            }
        }

        public async Task<TResult> MeasureActionAsync<TResult,TRecord>(
            TRecord record,
            Func<Task<TResult>> actionAsync,
            Func<TResult, string> actionResultToTelemetryStatus = null,
            Func<TResult, Task<long>> actionResultToItemCountAsync = null,
            Action<TResult, TRecord> updateRecord = null)

            where TRecord : ActionTelemetryRecord
        {
            ArgumentUtility.CheckForNull(record, nameof(record));
            ArgumentUtility.CheckForNull(actionAsync, nameof(actionAsync));

            if (actionResultToTelemetryStatus == null)
            {
                actionResultToTelemetryStatus = (TResult result) => ActionResult.Success.ToString();
            }

            if (actionResultToItemCountAsync == null)
            {
                actionResultToItemCountAsync = (TResult result) => Task.FromResult(0L);
            }

            if (updateRecord == null)
            {
                updateRecord = (TResult result, TRecord r) => { };
            }

            tracer.Verbose($"{record.ActionName} starting");

            var actionTimer = new Stopwatch();
            string telemetryStatus = null;
            long itemCount = 0;

            try
            {
                actionTimer.Start();

                TResult actionResult = await actionAsync().ConfigureAwait(false);

                telemetryStatus = actionResultToTelemetryStatus(actionResult);
                itemCount = await actionResultToItemCountAsync(actionResult).ConfigureAwait(false);
                updateRecord(actionResult, record);
                return actionResult;
            }
            catch (Exception exception)
            {
                actionTimer.Stop();
                tracer.Verbose(0, exception, $"{record.ActionName} failed after {actionTimer.Elapsed.TotalSeconds} seconds with {exception.GetType().Name}");

                var error = record.CaptureError(exception);
                ExecuteOnAllSendersWithExceptionLogging(sender => sender.SendErrorTelemetry(error));

                throw;
            }
            finally
            {
                actionTimer.Stop();
                tracer.Verbose($"{record.ActionName} completed in {actionTimer.Elapsed.TotalSeconds} seconds");

                record.CaptureResult(telemetryStatus, actionTimer, itemCount);
                ExecuteOnAllSendersWithExceptionLogging(sender => sender.SendActionTelemetry(record));
            }
        }

        public void AbortSend(TelemetryRecord record, string reason)
        {
            ArgumentUtility.CheckForNull(record, nameof(record));
            ArgumentUtility.CheckStringForNullOrEmpty(reason, nameof(reason));

            if (SendAborted != null)
            {
                SendAborted(record, reason);
            }
        }

        public event Action<TelemetryRecord, string> SendAborted;

        public void StopSenders()
        {
            foreach (var sender in this.senders)
            {
                try
                {
                    sender.StopSender();
                    this.tracer.Verbose($"Stopped {nameof(ITelemetrySender)} {sender.GetType().Name}.");
                }
                catch (Exception e)
                {
                    this.tracer.Warn(e, $"Ignored failure during {nameof(ITelemetrySender)}.{nameof(sender.StopSender)} for {sender.GetType().Name}.");
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    StopSenders();
                    this.senders.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
