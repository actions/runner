using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class JobExtensionRunner : IStep
    {
        private readonly Func<IExecutionContext, Task> _runAsync;

        public JobExtensionRunner(
            IExecutionContext context,
            Func<IExecutionContext, Task> runAsync,
            INode condition,
            string displayName)
        {
            ExecutionContext = context;
            _runAsync = runAsync;
            Condition = condition;
            DisplayName = displayName;
        }

        public INode Condition { get; set; }
        public bool ContinueOnError => false;
        public string DisplayName { get; private set; }
        public bool Enabled => true;
        public IExecutionContext ExecutionContext { get; set; }
        public TimeSpan? Timeout => null;

        public async Task RunAsync()
        {
            await _runAsync(ExecutionContext);
        }
    }
}
