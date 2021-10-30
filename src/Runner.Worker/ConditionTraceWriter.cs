using System;
using System.Text;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;

namespace GitHub.Runner.Worker
{
    public sealed class ConditionTraceWriter : ObjectTemplating::ITraceWriter
    {
        private readonly IExecutionContext _executionContext;
        private readonly Tracing _trace;
        private readonly StringBuilder _traceBuilder = new StringBuilder();

        public string Trace => _traceBuilder.ToString();

        public ConditionTraceWriter(Tracing trace, IExecutionContext executionContext)
        {
            ArgUtil.NotNull(trace, nameof(trace));
            _trace = trace;
            _executionContext = executionContext;
        }

        public void Error(string format, params Object[] args)
        {
            var message = StringUtil.Format(format, args);
            _trace.Error(message);
            _executionContext?.Debug(message);
        }

        public void Info(string format, params Object[] args)
        {
            var message = StringUtil.Format(format, args);
            _trace.Info(message);
            _executionContext?.Debug(message);
            _traceBuilder.AppendLine(message);
        }

        public void Verbose(string format, params Object[] args)
        {
            var message = StringUtil.Format(format, args);
            _trace.Verbose(message);
            _executionContext?.Debug(message);
        }
    }
}
