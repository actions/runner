using System;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// This is a temprary extension point for provider phase to participate in pipeline resource discover
    /// This extension point can be removed after we have the schema driven resource discover
    /// </summary>
    public interface IPhaseProvider
    {
        String Provider { get; }

        /// <summary>
        /// Validate pipeline with builder context to provide additional validation errors
        /// and pipeline resource discover.
        /// </summary>
        ValidationResult Validate(PipelineBuildContext context, ProviderPhase phase);
    }
}
