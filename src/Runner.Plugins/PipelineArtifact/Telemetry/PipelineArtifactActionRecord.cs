using System;
using GitHub.Runner.Sdk;
using GitHub.Services.Content.Common.Telemetry;
using GitHub.Services.BlobStore.Common.Telemetry;
using GitHub.Services.BlobStore.WebApi;

namespace GitHub.Runner.Plugins.PipelineArtifact.Telemetry
{
    /// <summary>
    /// Generic telemetry record for use with Pipeline Artifact events.
    /// </summary>
    public class PipelineArtifactActionRecord : PipelineTelemetryRecord
    {
        public static long FileCount { get; private set; }

        public PipelineArtifactActionRecord(TelemetryInformationLevel level, Uri baseAddress, string eventNamePrefix, string eventNameSuffix, RunnerActionPluginExecutionContext context, uint attemptNumber = 1)
            : base(level, baseAddress, eventNamePrefix, eventNameSuffix, context, attemptNumber)
        {
        }

        protected override void SetMeasuredActionResult<T>(T value)
        {
            if (value is PublishResult)
            {
                PublishResult result = value as PublishResult;
                FileCount = result.FileCount;
            }
        }
    }
}