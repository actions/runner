using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CLI
{
    public interface IStep
    {
        Boolean AlwaysRun { get; }
        Boolean ContinueOnError { get; }
        Boolean Critical { get; }
        String DisplayName { get; }
        Boolean Enabled { get; }
        String Id { get; }
        TaskResult? Result { get; set; }
        Task<TaskResult> RunAsync(IExecutionContext context);
    }
}
