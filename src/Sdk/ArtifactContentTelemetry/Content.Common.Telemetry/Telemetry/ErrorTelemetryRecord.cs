using System;
using System.Net.Http;
using GitHub.Services.Common;
using GitHub.Services.Content.Common.Tracing;

namespace GitHub.Services.Content.Common.Telemetry
{
    public class ErrorTelemetryRecord : TelemetryRecord
    {
        public string ActionName { get; set; }
        public string ItemName { get; set; }
        public string HttpRequestExceptionDetails { get; set; }

        public ErrorTelemetryRecord(TelemetryInformationLevel level, Uri baseAddress, Exception exception, string actionNameOptional, string itemNameOptional)
            : base(level, baseAddress)
        {
            ArgumentUtility.CheckForNull(exception, nameof(exception));
            this.Exception = exception;

            this.ActionName = actionNameOptional;
            this.ItemName = itemNameOptional;

            // Attach request exception details if any were recorded

            if (level >= TelemetryInformationLevel.FirstParty)
            {
                var requestException = exception as HttpRequestException;
                if (requestException != null)
                {
                    this.HttpRequestExceptionDetails = requestException.GetHttpMessageDetailsForTracing();
                }
            }
        }

        public ErrorTelemetryRecord(TelemetryRecord record) : this(record.Level, record.baseAddress, record.Exception, null, null)
        {
        }

        public ErrorTelemetryRecord(ActionTelemetryRecord record) : this(record.Level, record.baseAddress, record.Exception, record.ActionName, null)
        {
        }

        public ErrorTelemetryRecord(ItemTelemetryRecord record) : this(record.Level, record.baseAddress, record.Exception, record.ActionName, record.ItemName)
        {
        }
    }
}
