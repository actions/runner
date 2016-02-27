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
        Boolean AlwaysRun { get; }
        // Treat Failed as SucceededWithIssues.
        Boolean ContinueOnError { get; }
        // Treat failure as fatal. Subsequent AlwaysRun steps will not run.
        Boolean Critical { get; }
        String DisplayName { get; }
        Boolean Enabled { get; }
        // Always runs. Even if a previous critical step failed.
        Boolean Finally { get; }
        String Id { get; }
        TaskResult? Result { get; set; }
        Task<TaskResult> RunAsync(IExecutionContext context);
    }
        
    [ServiceLocator(Default = typeof(StepRunner))]
    public interface IStepRunner
    {
        Task<TaskResult> RunAsync(IExecutionContext context, IList<IStep> step);
    }

    public sealed class StepRunner : IStepRunner
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
                    step.Result = TaskResult.Skipped;
                    continue;
                }

                // Run the step.
                step.Result = await step.RunAsync(context);
                // TODO: Convert to trace: Console.WriteLine("Step result: {0}", step.Result);

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
