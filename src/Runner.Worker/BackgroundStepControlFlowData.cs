using System;

namespace GitHub.Runner.Worker
{
    /// <summary>
    /// Pure data for control-flow steps (wait, wait-all, cancel).
    /// Type uses Pipelines.BackgroundControlTypes string constants.
    /// </summary>
    public sealed class BackgroundStepControlFlowData
    {
        public string Type { get; set; }
        public Guid StepId { get; set; }
        public string StepName { get; set; }

        // Target step IDs (for wait: steps to wait for; for cancel: steps to cancel)
        public string[] StepIds { get; set; }

        // Parallel group ID for grouping steps in the UI
        public string ParallelGroupId { get; set; }
    }
}
