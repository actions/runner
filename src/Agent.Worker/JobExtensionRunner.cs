using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class JobExtensionRunner : IStep
    {
        private readonly Func<Task<TaskResult>> _runAsync;

        public JobExtensionRunner(
            Func<Task<TaskResult>> runAsync,
            bool alwaysRun,
            bool continueOnError,
            bool critical,
            string displayName,
            bool enabled,
            bool @finally)
        {
            _runAsync = runAsync;
            AlwaysRun = alwaysRun;
            ContinueOnError = continueOnError;
            Critical = critical;
            DisplayName = displayName;
            Enabled = enabled;
            Finally = @finally;
        }

        public bool AlwaysRun { get; private set; }
        public bool ContinueOnError { get; private set; }
        public bool Critical { get; private set; }
        public string DisplayName { get; private set; }
        public bool Enabled { get; private set; }
        public IExecutionContext ExecutionContext { get; set; }
        public bool Finally { get; private set; }
        public TaskResult? Result { get; set; }

        public Task<TaskResult> RunAsync()
        {
            return _runAsync();
        }
    }
}
