using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;

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
}
