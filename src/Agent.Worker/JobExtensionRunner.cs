using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class JobExtensionRunner : IStep
    {
        private readonly Func<Task> _runAsync;

        public JobExtensionRunner(
            Func<Task> runAsync,
            bool continueOnError,
            bool critical,
            string displayName,
            bool enabled,
            bool @finally)
        {
            _runAsync = runAsync;
            ContinueOnError = continueOnError;
            Critical = critical;
            DisplayName = displayName;
            Enabled = enabled;
            Finally = @finally;
        }

        public string Condition => Finally ? $"{Constants.Expressions.Always}()" : $"{Constants.Expressions.Succeeded}()";
        public bool ContinueOnError { get; private set; }
        public bool Critical { get; private set; }
        public string DisplayName { get; private set; }
        public bool Enabled { get; private set; }
        public IExecutionContext ExecutionContext { get; set; }
        public bool Finally { get; private set; }
        public TimeSpan? Timeout => null;

        public async Task RunAsync()
        {
            await _runAsync();
        }
    }
}
