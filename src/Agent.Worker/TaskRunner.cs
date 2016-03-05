using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskRunner))]
    public interface ITaskRunner : IStep, IAgentService
    {
        TaskInstance TaskInstance { get; set; }
    }

    public sealed class TaskRunner : AgentService, ITaskRunner
    {
        public bool AlwaysRun => TaskInstance?.AlwaysRun ?? default(bool);
        public bool ContinueOnError => TaskInstance?.ContinueOnError ?? default(bool);
        public bool Critical => false;
        public string DisplayName => TaskInstance?.DisplayName;
        public bool Enabled => TaskInstance?.Enabled ?? default(bool);
        public IExecutionContext ExecutionContext { get; set; }
        public bool Finally => false;
        public TaskInstance TaskInstance { get; set; }
        public TaskResult? Result { get; set; }

        public Task<TaskResult> RunAsync()
        {
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(TaskInstance, nameof(TaskInstance));
            // TODO: Load the task definition Path.Combine("tasks", TaskInstance.Name, taskInstance.Version)
            // TODO: Choose the handler.
            return Task.FromResult(TaskResult.Succeeded);
        }
    }
}
