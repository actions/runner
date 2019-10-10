using System;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.Runner.Worker
{
    public sealed class JobExtensionRunner : IStep
    {
        private readonly object _data;
        private readonly Func<IExecutionContext, object, Task> _runAsync;

        public JobExtensionRunner(
            Func<IExecutionContext, object, Task> runAsync,
            string condition,
            string displayName,
            object data)
        {
            _runAsync = runAsync;
            Condition = condition;
            DisplayName = displayName;
            _data = data;
        }

        public string Condition { get; set; }
        public TemplateToken ContinueOnError => new BooleanToken(null, null, null, false);
        public string DisplayName { get; set; }
        public IExecutionContext ExecutionContext { get; set; }
        public TemplateToken Timeout => new NumberToken(null, null, null, 0);
        public object Data => _data;

        public async Task RunAsync()
        {
            await _runAsync(ExecutionContext, _data);
        }
    }
}
