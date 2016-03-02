using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskRunner))]
    public interface ITaskRunner : IStep, IAgentService
    {
    }

    public sealed class TaskRunner : AgentService, ITaskRunner
    {
        // TODO: FIX THESE:
        public bool AlwaysRun => false;
        public bool ContinueOnError => false;
        public bool Critical => false;
        public string DisplayName => "Some display name";
        public bool Enabled => true;
        public IExecutionContext ExecutionContext { get; set; }
        public bool Finally => false;
        public string Id => Guid.NewGuid().ToString();
        public TaskResult? Result { get; set; }

        public async Task<TaskResult> RunAsync()
        {
            // TODO: IMPLEMENT
            await Task.Yield();
            return TaskResult.Succeeded;
        }
    }
}
