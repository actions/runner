using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitHub.Services.Content.Common.Telemetry;
using GitHub.Services.Content.Common.Tracing;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Wraps ClientTelemetry with BlobStore specific properties and actions.
    /// </summary>
    public class BlobStoreClientTelemetry : ClientTelemetry
    {
        internal const TelemetryInformationLevel TelemetryLevel = TelemetryInformationLevel.ThirdParty;

        public BlobStoreClientTelemetry(IAppTraceSource tracer, Uri baseAddress)
            : this(tracer, baseAddress, new BlobStoreApplicationInsightsTelemetrySender(tracer, baseAddress))
        {
        }

        public BlobStoreClientTelemetry(IAppTraceSource tracer, Uri baseAddress, BlobStoreApplicationInsightsTelemetrySender sender) : base(
            level: TelemetryLevel,
            vstsUri: baseAddress,
            tracer: tracer,
            enable: sender.InstrumentationKey != Guid.Empty,
            senderFactories: new Func<ITelemetrySender>[]
            {
                () => sender
            })
        {
        }

        // Used for testing only.
        internal BlobStoreClientTelemetry(IAppTraceSource tracer, Uri baseAddress, ITelemetrySender sender) : base(
            level: TelemetryLevel,
            vstsUri: baseAddress,
            tracer: tracer,
            enable: true,
            senderFactories: new Func<ITelemetrySender>[]
            {
                () => sender
            })
        {
        }

        public T CreateRecord<T>(Func<TelemetryInformationLevel, Uri, string, T> telemetryRecordFactory) where T : BlobStoreTelemetryRecord
        {
            return telemetryRecordFactory(this.Level, this.VstsUri, typeof(T).Name);
        }

        public Task MeasureActionAsync(BlobStoreTelemetryRecord record, Func<Task> actionAsync)
        {
            return base.MeasureActionAsync(
                record,
                async () =>
                {
                    await actionAsync().ConfigureAwait(false);
                    return (object)null;
                });
        }

        /// <summary>
        /// Applies action return values to the telemetry record before sending.
        /// Provided telemetry record must override SetReturnedProperty() for this to take effect.
        /// </summary>
        /// <typeparam name="TResult">Return type of some Task<TResult></typeparam>
        /// <param name="record">Telemetry record to store the returned value</param>
        /// <param name="actionAsync">The async action to measure</param>
        /// <returns></returns>
        public async Task<TResult> MeasureActionAsync<TResult>(BlobStoreTelemetryRecord record, Func<Task<TResult>> actionAsync)
        {
            Stopwatch timer = new Stopwatch();
            string actionResult = string.Empty;
            TResult returnValue;
            try
            {
                timer.Start();
                returnValue = await actionAsync().ConfigureAwait(false);
                record.SetMeasuredActionResult(returnValue);
                actionResult = ActionResult.Success.ToString();
            }
            catch (Exception e)
            {
                record.CaptureError(e);
                this.SendErrorTelemetry(e, record.ActionName);
                actionResult = ActionResult.Failure.ToString();
                throw;
            }
            finally
            {
                timer.Stop();

                if (string.IsNullOrEmpty(actionResult))
                {
                    actionResult = ActionResult.Unknown.ToString();
                }
                record.CaptureResult(actionResult, timer);
                this.SendRecord(record);
            }
            return returnValue;
        }

        public void SendErrorTelemetry(Exception exception, string actionName, string artifactNameOptional = null)
        {
            var errorTelemetry = new ErrorTelemetryRecord(this.Level, this.VstsUri, exception, actionName, artifactNameOptional);
            ExecuteOnAllSendersWithExceptionLogging(sender => sender.SendErrorTelemetry(errorTelemetry));
        }

        public void SendRecord(BlobStoreTelemetryRecord record)
        {
            ExecuteOnAllSendersWithExceptionLogging(sender => sender.SendActionTelemetry(record));
        }
    }
}
