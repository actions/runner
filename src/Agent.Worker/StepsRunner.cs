using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IStep
    {
        // Run even if a previous non-critical step has failed.
        bool AlwaysRun { get; }
        // Treat Failed as SucceededWithIssues.
        bool ContinueOnError { get; }
        // Treat failure as fatal. Subsequent AlwaysRun steps will not run.
        bool Critical { get; }
        string DisplayName { get; }
        bool Enabled { get; }
        IExecutionContext ExecutionContext { get; set; }
        // Always runs. Even if a previous critical step failed.
        bool Finally { get; }
        TaskResult? Result { get; set; }
        Task<TaskResult> RunAsync();
    }

    [ServiceLocator(Default = typeof(StepsRunner))]
    public interface IStepsRunner : IAgentService
    {
        Task<TaskResult> RunAsync(IExecutionContext context, IList<IStep> step);
    }

    public sealed class StepsRunner : AgentService, IStepsRunner
    {
        public async Task<TaskResult> RunAsync(IExecutionContext context, IList<IStep> steps)
        {
            // TODO: Convert to trace: Console.WriteLine("Steps.Count: {0}", steps.Count);
            // TaskResult:
            //  Abandoned
            //  Canceled
            //  Failed
            //  Skipped
            //  Succeeded
            //  SucceededWithIssues
            TaskResult jobResult = TaskResult.Succeeded;
            Boolean stepFailed = false;
            Boolean criticalStepFailed = false;
            foreach (IStep step in steps)
            {
                // Skip the current step if it is not Enabled.
                if (!step.Enabled
                    // Or if a previous step failed and the current step is not AlwaysRun.
                    || (stepFailed && !step.AlwaysRun && !step.Finally)
                    // Or if a previous Critical step failed and the current step is not Finally.
                    || (criticalStepFailed && !step.Finally))
                {
                    Trace.Verbose($"Skipping '{step.DisplayName}'.");
                    step.Result = TaskResult.Skipped;
                    continue;
                }

                // Run the step.
                Trace.Verbose($"Running '{step.DisplayName}'.");
                step.Result = await step.RunAsync();
                Trace.Verbose($"Step result: {step.Result}");

                // Fixup the step result if ContinueOnError.
                if (step.Result.Value == TaskResult.Failed && step.ContinueOnError)
                {
                    step.Result = TaskResult.SucceededWithIssues;
                }

                // Update the step failed flags.
                stepFailed = stepFailed || step.Result.Value == TaskResult.Failed;
                criticalStepFailed = criticalStepFailed || (step.Critical && step.Result.Value == TaskResult.Failed);

                // Update the job result.
                if (step.Result.Value == TaskResult.Failed)
                {
                    jobResult = TaskResult.Failed;
                }
                else if (jobResult == TaskResult.Succeeded && step.Result.Value == TaskResult.SucceededWithIssues)
                {
                    jobResult = TaskResult.SucceededWithIssues;
                }
            }

            return jobResult;
        }
    }
}
