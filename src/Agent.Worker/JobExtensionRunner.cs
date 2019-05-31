using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating;
using Microsoft.VisualStudio.Services.Agent.Util;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker
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
