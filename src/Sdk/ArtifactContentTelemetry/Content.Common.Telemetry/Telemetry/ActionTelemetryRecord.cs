using System;
using System.Diagnostics;
using GitHub.Services.Common;

namespace GitHub.Services.Content.Common.Telemetry
{
    public class ActionTelemetryRecord : TelemetryRecord
    {
#if DEBUG
        private bool capturedError;
#endif
        private bool capturedResult;

        public long ActionDurationMs { get; private set; }
        public string ActionName { get; private set; }
        public string ActionResult { get; private set; }
        public uint AttemptNumber { get; private set; }
        public long ItemCount { get; private set; }

        public ActionTelemetryRecord(TelemetryInformationLevel level, Uri baseAddress, string actionName, uint attemptNumber = 1)
            : base(level, baseAddress)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(actionName, nameof(actionName));
            if (attemptNumber == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptNumber), $"Expected a positive value, but {nameof(attemptNumber)} was zero");
            }

            this.ActionName = actionName;
            this.AttemptNumber = attemptNumber;
        }

        public ActionTelemetryRecord(ActionTelemetryRecord record) : base(record)
        {
            this.ActionName = record.ActionName;
            this.ItemCount = record.ItemCount;
            this.AttemptNumber = record.AttemptNumber;
        }

        public virtual ErrorTelemetryRecord CaptureError(Exception exception)
        {
#if DEBUG
            // In debug, surface collisions, but in release overwrite with the last error
            if (capturedError)
            {
                throw new InvalidOperationException($"{nameof(CaptureError)} has already been called for action {this.ActionName}");
            }

            capturedError = true;
#endif
            this.Exception = exception;

            if (exception.InnerException is TimeoutException)
            {
                this.ActionResult = "Timeout";
            }
            else
            {
                this.ActionResult = exception.Message;
            }

            return new ErrorTelemetryRecord(this);
        }

        public virtual void CaptureResult(string actionResult, Stopwatch actionTimer, long itemCount = 0)
        {
            if (capturedResult)
            {
                throw new InvalidOperationException($"{nameof(CaptureResult)} has already been called for action {this.ActionName}");
            }

            capturedResult = true;

            // override error/existing action result
            if (!string.IsNullOrEmpty(actionResult))
            {
                this.ActionResult = actionResult;
            }

            if (actionTimer.IsRunning)
            {
                actionTimer.Stop();
            }

            this.ActionDurationMs = actionTimer.ElapsedMilliseconds;

            this.ItemCount = itemCount;
        }
    }
}
