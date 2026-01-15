using System;
using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Represents a snapshot of job state captured just before a step executes.
    /// Created when user issues next/continue command, after any REPL modifications.
    /// Used for step-back (time-travel) debugging.
    /// </summary>
    public sealed class StepCheckpoint
    {
        /// <summary>
        /// Index of this checkpoint in the checkpoints list.
        /// Used when restoring to identify which checkpoint to restore to.
        /// </summary>
        public int CheckpointIndex { get; set; }

        /// <summary>
        /// Zero-based index of the step in the job.
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// Display name of the step this checkpoint was created for.
        /// </summary>
        public string StepDisplayName { get; set; }

        /// <summary>
        /// Snapshot of Global.EnvironmentVariables.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Snapshot of ExpressionValues["env"] context data.
        /// </summary>
        public Dictionary<string, string> EnvContextData { get; set; }

        /// <summary>
        /// Snapshot of Global.PrependPath.
        /// </summary>
        public List<string> PrependPath { get; set; }

        /// <summary>
        /// Snapshot of job result.
        /// </summary>
        public TaskResult? JobResult { get; set; }

        /// <summary>
        /// Snapshot of job status.
        /// </summary>
        public ActionResult? JobStatus { get; set; }

        /// <summary>
        /// Snapshot of steps context (outputs, outcomes, conclusions).
        /// Key is "{scopeName}/{stepName}", value is the step's state.
        /// </summary>
        public Dictionary<string, StepStateSnapshot> StepsSnapshot { get; set; }

        /// <summary>
        /// The step that was about to execute (for re-running).
        /// </summary>
        public IStep CurrentStep { get; set; }

        /// <summary>
        /// Steps remaining in the queue after CurrentStep.
        /// </summary>
        public List<IStep> RemainingSteps { get; set; }

        /// <summary>
        /// When this checkpoint was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Snapshot of a single step's state in the steps context.
    /// </summary>
    public sealed class StepStateSnapshot
    {
        public ActionResult? Outcome { get; set; }
        public ActionResult? Conclusion { get; set; }
        public Dictionary<string, string> Outputs { get; set; }
    }
}
