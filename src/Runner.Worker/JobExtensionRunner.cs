using System;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using Runner.Common.Util;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using Runner.Common.Worker.Handlers;
using System.Collections.Generic;
using System.IO;

namespace Runner.Common.Worker
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

        public async Task RunAsync()
        {
            await _runAsync(ExecutionContext, _data);
        }
    }
}
