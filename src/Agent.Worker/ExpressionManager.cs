using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using DT = Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ExpressionManager))]
    public interface IExpressionManager : IAgentService
    {
        bool Evaluate(IExecutionContext context, string condition);
    }

    public sealed class ExpressionManager : AgentService, IExpressionManager
    {
        public bool Evaluate(IExecutionContext executionContext, string condition)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            // Parse the condition.
            var expressionTrace = new TraceWriter(executionContext);
            var parser = new DT.Parser();
            var namedValues = new DT.INamedValueInfo[]
            {
                new DT.NamedValueInfo<VariablesNode>(name: Constants.Expressions.Variables),
            };
            var functions = new DT.IFunctionInfo[]
            {
                new DT.FunctionInfo<SucceededNode>(name: Constants.Expressions.Succeeded, minParameters: 0, maxParameters: 0),
                new DT.FunctionInfo<SucceededOrFailedNode>(name: Constants.Expressions.SucceededOrFailed, minParameters: 0, maxParameters: 0),
            };
            DT.INode tree = parser.CreateTree(condition, expressionTrace, namedValues, functions) ?? new SucceededNode();

            // Evaluate the tree.
            return tree.EvaluateBoolean(trace: expressionTrace, state: executionContext);
        }

        private sealed class TraceWriter : DT.ITraceWriter
        {
            private readonly IExecutionContext _executionContext;

            public TraceWriter(IExecutionContext executionContext)
            {
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                _executionContext = executionContext;
            }

            public void Info(string message)
            {
                _executionContext.Debug(message);
            }

            public void Verbose(string message)
            {
                _executionContext.Debug(message);
            }
        }

        private sealed class SucceededNode : DT.FunctionNode
        {
            public sealed override string Name => Constants.Expressions.Succeeded;

            protected sealed override object EvaluateCore(DT.EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.Variables.Agent_JobStatus ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Succeeded ||
                    jobStatus == TaskResult.SucceededWithIssues;
            }
        }

        private sealed class SucceededOrFailedNode : DT.FunctionNode
        {
            public sealed override string Name => Constants.Expressions.SucceededOrFailed;

            protected sealed override object EvaluateCore(DT.EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                TaskResult jobStatus = executionContext.Variables.Agent_JobStatus ?? TaskResult.Succeeded;
                return jobStatus == TaskResult.Succeeded ||
                    jobStatus == TaskResult.SucceededWithIssues ||
                    jobStatus == TaskResult.Failed;
            }
        }

        private sealed class VariablesNode : DT.NamedValueNode
        {
            public sealed override string Name => Constants.Expressions.Variables;

            protected sealed override object EvaluateCore(DT.EvaluationContext evaluationContext)
            {
                var executionContext = evaluationContext.State as IExecutionContext;
                ArgUtil.NotNull(executionContext, nameof(executionContext));
                return new VariablesDictionary(executionContext.Variables);
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
}