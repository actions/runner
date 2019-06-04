using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Pipelines.Expressions;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class GraphCondition<TInstance> where TInstance : IGraphNodeInstance
    {
        private protected GraphCondition(String condition)
        {
            m_condition = !String.IsNullOrEmpty(condition) ? condition : Default;
            m_parser = new ExpressionParser();
            m_parsedCondition = m_parser.CreateTree(m_condition, new ConditionTraceWriter(), s_namedValueInfo, s_functionInfo);
            m_requiresOutputs = new Lazy<Boolean>(HasOutputsReferences);
            m_requiresVariables = new Lazy<Boolean>(HasVariablesReference);
        }

        /// <summary>
        /// Gets the default condition if none is specified
        /// </summary>
        public static String Default
        {
            get
            {
                return "succeeded()";
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not dependency outputs are used within the condition.
        /// </summary>
        public Boolean RequiresOutputs
        {
            get
            {
                return m_requiresOutputs.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not variables are used within the condition.
        /// </summary>
        public Boolean RequiresVariables
        {
            get
            {
                return m_requiresVariables.Value;
            }
        }

        private Boolean HasOutputsReferences()
        {
            var dependencies = m_parsedCondition.GetParameters<DependenciesContextNode<TInstance>>().ToList();
            if (dependencies.Count == 0)
            {
                return false;
            }

            foreach (var dependencyNode in dependencies)
            {
                var propertyIndexer = dependencyNode.Container?.Container;
                if (propertyIndexer != null)
                {
                    var propertyName = propertyIndexer.Parameters.OfType<LiteralValueNode>().FirstOrDefault();
                    if (propertyName != null)
                    {
                        if (propertyName.Kind == ValueKind.String)
                        {
                            var propertyNameValue = (String)propertyName.Value;
                            return propertyNameValue.Equals("outputs", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }

            return false;
        }

        private Boolean HasVariablesReference()
        {
            return m_parsedCondition.GetParameters<VariablesContextNode>().Any();
        }

        private static IEnumerable<IGraphNodeInstance> GetNodesForEvaluation(
            FunctionNode function,
            EvaluationContext context,
            GraphExecutionContext<TInstance> expressionContext)
        {
            if (function.Parameters.Count == 0)
            {
                foreach (var dependency in expressionContext.Dependencies)
                {
                    yield return dependency.Value;
                }
            }
            else
            {
                foreach (var dependencyName in function.Parameters)
                {
                    TInstance node;
                    var dependencyNameValue = dependencyName.EvaluateString(context);
                    if (!expressionContext.Dependencies.TryGetValue(dependencyNameValue, out node))
                    {
                        yield return default;
                    }
                    else
                    {
                        yield return node;
                    }
                }
            }
        }

        private readonly String m_condition;
        private readonly ExpressionParser m_parser;
        protected readonly IExpressionNode m_parsedCondition;
        private readonly Lazy<Boolean> m_requiresOutputs;
        private readonly Lazy<Boolean> m_requiresVariables;

        private static readonly INamedValueInfo[] s_namedValueInfo = new INamedValueInfo[]
        {
            Expressions.ExpressionConstants.PipelineNamedValue,
            Expressions.ExpressionConstants.VariablesNamedValue,
            new NamedValueInfo<DependenciesContextNode<TInstance>>(Expressions.ExpressionConstants.Dependencies),
        };

        private static readonly IFunctionInfo[] s_functionInfo = new IFunctionInfo[]
        {
            new FunctionInfo<AlwaysNode>("always", 0, 0),
            new FunctionInfo<FailedNode>("failed", 0, Int32.MaxValue),
            new FunctionInfo<CanceledNode>("canceled", 0, 0),
            new FunctionInfo<SucceededNode>("succeeded", 0, Int32.MaxValue),
            new FunctionInfo<SucceededOrFailedNode>("succeededOrFailed", 0, Int32.MaxValue),
        };

        protected sealed class ConditionTraceWriter : ITraceWriter
        {
            public String Trace
            {
                get
                {
                    return m_info.ToString();
                }
            }

            public void Info(String message)
            {
                m_info.AppendLine(message);
            }

            public void Verbose(String message)
            {
                // Not interested 
            }

            private StringBuilder m_info = new StringBuilder();
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
            protected override Object EvaluateCore(EvaluationContext context)
            {
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                return conditionContext.State == PipelineState.Canceling;
            }
        }

        private sealed class FailedNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                if (conditionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                Boolean anyFailed = false;
                foreach (var node in GetNodesForEvaluation(this, context, conditionContext))
                {
                    if (node == null)
                    {
                        return false;
                    }

                    if (node.Result == TaskResult.Failed)
                    {
                        anyFailed = true;
                        break;
                    }
                }

                return anyFailed;
            }
        }

        private sealed class SucceededNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                if (conditionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                Boolean allSucceeded = true;
                foreach (var node in GetNodesForEvaluation(this, context, conditionContext))
                {
                    if (!allSucceeded || node == null)
                    {
                        return false;
                    }

                    allSucceeded &= (node.Result == TaskResult.Succeeded || node.Result == TaskResult.SucceededWithIssues);
                }

                return allSucceeded;
            }
        }

        private sealed class SucceededOrFailedNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                if (conditionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                Boolean anyFailed = false;
                Boolean allSucceeded = true;
                foreach (var node in GetNodesForEvaluation(this, context, conditionContext))
                {
                    if (node == null)
                    {
                        return false;
                    }

                    if (node.Result == TaskResult.Failed)
                    {
                        anyFailed = true;
                        break;
                    }
                    else
                    {
                        allSucceeded = (node.Result == TaskResult.Succeeded || node.Result == TaskResult.SucceededWithIssues);
                    }
                }

                return anyFailed || allSucceeded;
            }
        }
    }
}
