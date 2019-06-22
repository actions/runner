using System;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Content.Common.Telemetry;
using GitHub.Services.BlobStore.Common.Telemetry;
using GitHub.Services.Common;

namespace GitHub.Runner.Plugins.PipelineArtifact.Telemetry
{
    /// <summary>
    /// Generic telemetry record for use with Pipeline events.
    /// </summary>
    public abstract class PipelineTelemetryRecord : BlobStoreTelemetryRecord
    {
        public Guid PlanId { get; private set; }
        public Guid JobId { get; private set; }
        public Guid TaskInstanceId { get; private set; }

        public PipelineTelemetryRecord(TelemetryInformationLevel level, Uri baseAddress, string eventNamePrefix, string eventNameSuffix, RunnerActionPluginExecutionContext context, uint attemptNumber = 1)
            : base(level, baseAddress, eventNamePrefix, eventNameSuffix, attemptNumber)
        {
            PlanId = new Guid(context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.PlanId)?.Value ?? Guid.Empty.ToString());
            JobId = new Guid(context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.JobId)?.Value ?? Guid.Empty.ToString());
            TaskInstanceId = new Guid(context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.TaskInstanceId)?.Value ?? Guid.Empty.ToString());
        }
    }
}