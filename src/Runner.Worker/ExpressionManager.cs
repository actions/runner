using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ExpressionManager))]
    public interface IExpressionManager : IRunnerService
    {
        IExpressionNode Parse(IExecutionContext context, string condition);
        ConditionResult Evaluate(IExecutionContext context, IExpressionNode tree, bool hostTracingOnly = false);
    }

    public sealed class ExpressionManager : RunnerService, IExpressionManager
    {
        public static IExpressionNode Always = new AlwaysNode();
        public static IExpressionNode Succeeded = new SucceededNode();
        public static IExpressionNode SucceededOrFailed = new SucceededOrFailedNode();

        public IExpressionNode Parse(IExecutionContext executionContext, string condition)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            var expressionTrace = new TraceWriter(Trace, executionContext);
            var parser = new ExpressionParser();
            var namedValues = executionContext.ExpressionValues.Keys.Select(x => new NamedValueInfo<ContextValueNode>(x)).ToArray();

            var functions = new IFunctionInfo[]
            {
                new FunctionInfo<AlwaysNode>(name: Constants.Expressions.Always, minParameters: 0, maxParameters: 0),
                new FunctionInfo<CanceledNode>(name: Constants.Expressions.Canceled, minParameters: 0, maxParameters: 0),
                new FunctionInfo<FailedNode>(name: Constants.Expressions.Failed, minParameters: 0, maxParameters: 0),
                new FunctionInfo<SucceededNode>(name: Constants.Expressions.Succeeded, minParameters: 0, maxParameters: 0),
                new FunctionInfo<SucceededOrFailedNode>(name: Constants.Expressions.SucceededOrFailed, minParameters: 0, maxParameters: 0),
            };
            return parser.CreateTree(condition, expressionTrace, namedValues, functions) ?? new SucceededNode();
        }

        public ConditionResult Evaluate(IExecutionContext executionContext, IExpressionNode tree, bool hostTracingOnly = false)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(tree, nameof(tree));

            ConditionResult result = new ConditionResult();
            var expressionTrace = new TraceWriter(Trace, hostTracingOnly ? null : executionContext);
            var expressionResult = tree.Evaluate(expressionTrace, HostContext.SecretMasker, state: executionContext, options: null);
            result.Value = expressionResult.IsTruthy;
            result.Trace = expressionTrace.Trace;

            return result;
        }

        private sealed class TraceWriter : DistributedTask.Expressions2.ITraceWriter
        {
            private readonly IExecutionContext _executionContext;
            private readonly Tracing _trace;
            private readonly StringBuilder _traceBuilder = new StringBuilder();

            public string Trace => _traceBuilder.ToString();

            public TraceWriter(Tracing trace, IExecutionContext executionContext)
            {
                ArgUtil.NotNull(trace, nameof(trace));
                _trace = trace;
                _executionContext = executionContext;
            }

            public void Info(string message)
            {
                _trace.Info(message);
                _executionContext?.Debug(message);
                _traceBuilder.AppendLine(message);
            }

            public void Verbose(string message)
            {
                _trace.Verbose(message);
                _executionContext?.Debug(message);
            }
        }

        private sealed class AlwaysNode : Function
        {
            protected override Object EvaluateCore(EvaluationContext context, out ResultMemory resultMemory)
            {
                resultMemory = null;
                return true;
            }
        }

        private sealed class CanceledNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.JobContext.Status ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Canceled;
            }
        }

        private sealed class FailedNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.JobContext.Status ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Failed;
            }
        }

        private sealed class SucceededNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.JobContext.Status ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Succeeded;
            }
        }

        private sealed class SucceededOrFailedNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.JobContext.Status ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Succeeded ||
                    jobStatus == TaskResult.Failed;
            }
        }

        private sealed class ContextValueNode : NamedValue
        {
            protected override Object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var jobContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(jobContext, nameof(jobContext));
                return jobContext.ExpressionValues[Name];
            }
        }
    }

    public class ConditionResult
    {
        public ConditionResult(bool value = false, string trace = null)
        {
            this.Value = value;
            this.Trace = trace;
        }

        public bool Value { get; set; }
        public string Trace { get; set; }

        public static implicit operator ConditionResult(bool value)
        {
            return new ConditionResult(value);
        }
    }
}
