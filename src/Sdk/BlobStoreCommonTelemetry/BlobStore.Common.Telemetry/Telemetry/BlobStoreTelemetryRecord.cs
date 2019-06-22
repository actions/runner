using System;
using GitHub.Services.Content.Common.Telemetry;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Abstract telemetry record for use with BlobStore events.
    /// </summary>
    public abstract class BlobStoreTelemetryRecord : ActionTelemetryRecord
    {
        public static string EventNamePrefix { get; private set; }
        public static string EventNameSuffix { get; private set; }
        private const string ActionRecord = "ActionRecord";

        public BlobStoreTelemetryRecord(TelemetryInformationLevel level, Uri baseAddress, string eventNamePrefix, string eventNameSuffix, uint attemptNumber = 1)
            : base(level, baseAddress, $"{TrimPrefix(eventNamePrefix)}.{eventNameSuffix}", attemptNumber)
        {
            EventNamePrefix = eventNamePrefix;
            EventNameSuffix = eventNameSuffix;
        }

        /// <summary>
        /// Allows records to assign properties based on the return value of some task.
        /// Virtual so it's not required in derived classes, and this op can gain access to various types without taking dependencies.
        /// </summary>
        /// <typeparam name="T">Return type of some Task<TResult></typeparam>
        /// <param name="value">Return value of the Task to be assigned</param>
        protected internal virtual void SetMeasuredActionResult<T>(T value)
        {
            // No op
        }

        /// <summary>
        /// Trims unwanted suffixes from TelemetryRecords.
        /// "PipelineArtifactActionRecord" => "PipelineArtifact"
        /// </summary>
        /// <param name="prefix">The type name of telemetry record being created e.g. "PipelineArtifactActionRecord".</param>
        /// <returns>Trimmed type name as string.</returns>
        private static string TrimPrefix(string prefix)
        {
            string trimmedTypeName = prefix;
            string[] suffixes = { nameof(TelemetryRecord), ActionRecord };
            foreach (string suffix in suffixes)
            {
                int suffixIndex = trimmedTypeName.IndexOf(suffix);
                if (suffixIndex >= 0)
                {
                    trimmedTypeName = trimmedTypeName.Substring(0, suffixIndex);
                }
            }
            return trimmedTypeName;
        }
    }
}
