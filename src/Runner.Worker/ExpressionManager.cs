using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using PipelineTemplateConstants = GitHub.DistributedTask.Pipelines.ObjectTemplating.PipelineTemplateConstants;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ExpressionManager))]
    public interface IExpressionManager : IRunnerService
    {
        ConditionResult Evaluate(IExecutionContext context, string condition, bool hostTracingOnly = false);
    }

    public sealed class ExpressionManager : RunnerService, IExpressionManager
    {
        public ConditionResult Evaluate(IExecutionContext executionContext, string condition, bool hostTracingOnly = false)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            ConditionResult result = new ConditionResult();
            var expressionTrace = new TraceWriter(Trace, hostTracingOnly ? null : executionContext);
            var tree = Parse(executionContext, expressionTrace, condition);
            var expressionResult = tree.Evaluate(expressionTrace, HostContext.SecretMasker, state: executionContext, options: null);
            result.Value = expressionResult.IsTruthy;
            result.Trace = expressionTrace.Trace;

            return result;
        }

        private static IExpressionNode Parse(IExecutionContext executionContext, TraceWriter expressionTrace, string condition)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            if (string.IsNullOrWhiteSpace(condition))
            {
                condition = $"{PipelineTemplateConstants.Success}()";
            }

            var parser = new ExpressionParser();
            var namedValues = executionContext.ExpressionValues.Keys.Select(x => new NamedValueInfo<ContextValueNode>(x)).ToArray();
            var functions = new IFunctionInfo[]
            {
                new FunctionInfo<AlwaysNode>(name: Constants.Expressions.Always, minParameters: 0, maxParameters: 0),
                new FunctionInfo<CancelledNode>(name: Constants.Expressions.Cancelled, minParameters: 0, maxParameters: 0),
                new FunctionInfo<FailureNode>(name: Constants.Expressions.Failure, minParameters: 0, maxParameters: 0),
                new FunctionInfo<SuccessNode>(name: Constants.Expressions.Success, minParameters: 0, maxParameters: 0),
            };
            return parser.CreateTree(condition, expressionTrace, namedValues, functions) ?? new SuccessNode();
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

        private sealed class CancelledNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                ActionResult jobStatus = executionContext.JobContext.Status ?? ActionResult.Success;
                return jobStatus == ActionResult.Cancelled;
            }
        }

        private sealed class FailureNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                ActionResult jobStatus = executionContext.JobContext.Status ?? ActionResult.Success;
                return jobStatus == ActionResult.Failure;
            }
        }

        private sealed class SuccessNode : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                ActionResult jobStatus = executionContext.JobContext.Status ?? ActionResult.Success;
                return jobStatus == ActionResult.Success;
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
