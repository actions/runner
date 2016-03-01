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
        public bool AlwaysRun { get { throw new NotImplementedException(); } }
        public bool ContinueOnError { get { throw new NotImplementedException(); } }
        public bool Critical { get { throw new NotImplementedException(); } }
        public string DisplayName { get { throw new NotImplementedException(); } }
        public bool Enabled { get { throw new NotImplementedException(); } }
        public IExecutionContext ExecutionContext { get; set; }
        public bool Finally { get { throw new NotImplementedException(); } }
        public string Id { get { throw new NotImplementedException(); } }
        public TaskResult? Result { get; set; }

        public async Task<TaskResult> RunAsync()
        {
            await Task.Yield();
            throw new NotImplementedException();
        }
    }
}
