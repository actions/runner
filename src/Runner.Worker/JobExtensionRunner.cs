using System;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;

namespace GitHub.Runner.Worker
{
    public sealed class JobExtensionRunner : IStep
    {
        private readonly object _data;
        private readonly Func<IExecutionContext, object, Task> _runAsync;

        public JobExtensionRunner(
            Func<IExecutionContext, object, Task> runAsync,
            IExpressionNode condition,
            string displayName,
            object data)
        {
            _runAsync = runAsync;
            Condition = condition;
            DisplayName = displayName;
            _data = data;
        }

        public IExpressionNode Condition { get; set; }
        public bool ContinueOnError => false;
        public string DisplayName { get; private set; }
        public bool Enabled => true;
        public IExecutionContext ExecutionContext { get; set; }
        public TimeSpan? Timeout => null;
        public object Data => _data;

        public async Task RunAsync()
        {
            await _runAsync(ExecutionContext, _data);
        }
    }
}
