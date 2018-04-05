using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ExpressionManager))]
    public interface IExpressionManager : IAgentService
    {
        IExpressionNode Parse(IExecutionContext context, string condition);
        ConditionResult Evaluate(IExecutionContext context, IExpressionNode tree, bool hostTracingOnly = false);
    }

    public sealed class ExpressionManager : AgentService, IExpressionManager
    {
        public static IExpressionNode Always = new AlwaysNode();
        public static IExpressionNode Succeeded = new SucceededNode();
        public static IExpressionNode SucceededOrFailed = new SucceededOrFailedNode();

        public IExpressionNode Parse(IExecutionContext executionContext, string condition)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            var expressionTrace = new TraceWriter(Trace, executionContext);
            var parser = new ExpressionParser();
            var namedValues = new INamedValueInfo[]
            {
                new NamedValueInfo<VariablesNode>(name: Constants.Expressions.Variables),
            };
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

            result.Value = tree.Evaluate<bool>(trace: expressionTrace, secretMasker: HostContext.SecretMasker, state: executionContext);
            result.Trace = expressionTrace.Trace;

            return result;
        }

        private sealed class TraceWriter : ITraceWriter
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

        private sealed class AlwaysNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                return true;
            }
        }

        private sealed class CanceledNode : FunctionNode
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.Variables.Agent_JobStatus ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Canceled;
            }
        }

        private sealed class FailedNode : FunctionNode
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.Variables.Agent_JobStatus ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Failed;
            }
        }

        private sealed class SucceededNode : FunctionNode
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.Variables.Agent_JobStatus ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Succeeded ||
                    jobStatus == TaskResult.SucceededWithIssues;
            }
        }

        private sealed class SucceededOrFailedNode : FunctionNode
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.Variables.Agent_JobStatus ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Succeeded ||
                    jobStatus == TaskResult.SucceededWithIssues ||
                    jobStatus == TaskResult.Failed;
            }
        }

        private sealed class VariablesNode : NamedValueNode
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext)
            {
                var jobContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(jobContext, nameof(jobContext));
                return new VariablesDictionary(jobContext.Variables);
            }
        }

        private sealed class VariablesDictionary : IReadOnlyDictionary<string, object>
        {
            private readonly Variables _variables;

            public VariablesDictionary(Variables variables)
            {
                _variables = variables;
            }

            // IReadOnlyDictionary<string object> members
            public object this[string key] => _variables.Get(key);

            public IEnumerable<string> Keys => throw new NotSupportedException();

            public IEnumerable<object> Values => throw new NotSupportedException();

            public bool ContainsKey(string key)
            {
                string val;
                return _variables.TryGetValue(key, out val);
            }

            public bool TryGetValue(string key, out object value)
            {
                string s;
                bool found = _variables.TryGetValue(key, out s);
                value = s;
                return found;
            }

            // IReadOnlyCollection<KeyValuePair<string, object>> members
            public int Count => throw new NotSupportedException();

            // IEnumerable<KeyValuePair<string, object>> members
            IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => throw new NotSupportedException();


            // IEnumerable members
            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
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