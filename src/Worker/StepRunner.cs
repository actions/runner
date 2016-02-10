using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CLI
{
    [ServiceLocator(Default = typeof(StepRunner))]
    public interface IStepRunner
    {
        Task<TaskResult> RunAsync(IExecutionContext context, IList<IStep> step);
    }

    public sealed class StepRunner : IStepRunner
    {
        public async Task<TaskResult> RunAsync(IExecutionContext context, IList<IStep> steps)
        {
            // TaskResult:
            //  Abandoned
            //  Canceled
            //  Failed
            //  Skipped
            //  Succeeded
            //  SucceededWithIssues
            TaskResult? jobResult = null;
            Boolean criticalFailed = false;
            foreach (IStep step in steps)
            {
                // Skip if the step is disabled.
                if (!step.Enabled
                    // Or if the job is critical failed and the step is not critical.
                    || (criticalFailed && !step.Critical)
                    // Or if the job is failed and the step is not always run.
                    || (jobResult.HasValue && jobResult.Value == TaskResult.Failed && !step.AlwaysRun))
                {
                    step.Result = TaskResult.Skipped;
                    continue;
                }

                // Run the step.
                step.Result = await step.RunAsync(context);

                // Override the job result if the step failed.
                if (step.Result.Value == TaskResult.Failed
                    // Or if the job is null or succeeded, and the step succeeded with issues.
                    || ((jobResult ?? TaskResult.Succeeded) == TaskResult.Succeeded && step.Result.Value == TaskResult.SucceededWithIssues))
                {
                    jobResult = step.Result;
                }

                // Update the critical failed flag.
                if (step.Critical && step.Result.Value == TaskResult.Failed)
                {
                    jobResult = TaskResult.Failed;
                }
            }

            // Default the job result to succeeded.
            return jobResult ?? TaskResult.Succeeded;
        }
    }
}
