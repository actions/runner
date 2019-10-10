using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.Expressions;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.Pipelines.Runtime;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class GraphCondition<TInstance> where TInstance : IGraphNodeInstance
    {
        private protected GraphCondition(String condition)
        {
            m_condition = !String.IsNullOrEmpty(condition) ? condition : Default;
            m_parser = new ExpressionParser();
            m_parsedCondition = m_parser.CreateTree(m_condition, new ConditionTraceWriter(), s_namedValueInfo, FunctionInfo);
        }

        /// <summary>
        /// Gets the default condition if none is specified
        /// </summary>
        public static String Default
        {
            get
            {
                return $"{PipelineTemplateConstants.Success}()";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the event payload is used within the condition
        /// </summary>
        public Boolean RequiresEventPayload
        {
            get
            {
                CheckRequiredProperties();
                return m_requiresEventPayload.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether dependency outputs are used within the condition
        /// </summary>
        public Boolean RequiresOutputs
        {
            get
            {
                CheckRequiredProperties();
                return m_requiresOutputs.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether variables are used within the condition
        /// </summary>
        public Boolean RequiresVariables
        {
            get
            {
                return false;
            }
        }

        private void CheckRequiredProperties()
        {
            var matches = m_parsedCondition.CheckReferencesContext(PipelineTemplateConstants.EventPattern, PipelineTemplateConstants.OutputsPattern);
            m_requiresEventPayload = matches[0];
            m_requiresOutputs = matches[1];
        }

        private static IEnumerable<DictionaryContextData> GetNeeds(
            IReadOnlyList<ExpressionNode> parameters,
            EvaluationContext context,
            GraphExecutionContext<TInstance> expressionContext)
        {
            if (expressionContext.Data.TryGetValue(PipelineTemplateConstants.Needs, out var needsData) &&
                needsData is DictionaryContextData needs)
            {
                if (parameters.Count == 0)
                {
                    foreach (var pair in needs)
                    {
                        yield return pair.Value as DictionaryContextData;
                    }
                }
                else
                {
                    foreach (var parameter in parameters)
                    {
                        var parameterResult = parameter.Evaluate(context);
                        var dependencyName = default(String);
                        if (parameterResult.IsPrimitive)
                        {
                            dependencyName = parameterResult.ConvertToString();
                        }

                        if (!String.IsNullOrEmpty(dependencyName) &&
                            needs.TryGetValue(dependencyName, out var need))
                        {
                            yield return need as DictionaryContextData;
                        }
                        else
                        {
                            yield return default;
                        }
                    }
                }
            }
        }

        private readonly String m_condition;
        private readonly ExpressionParser m_parser;
        private Boolean? m_requiresEventPayload;
        private Boolean? m_requiresOutputs;
        protected readonly IExpressionNode m_parsedCondition;

        private static readonly INamedValueInfo[] s_namedValueInfo = new INamedValueInfo[]
        {
            new NamedValueInfo<GraphConditionNamedValue<TInstance>>(PipelineTemplateConstants.GitHub),
            new NamedValueInfo<GraphConditionNamedValue<TInstance>>(PipelineTemplateConstants.Needs),
        };

        public static readonly IFunctionInfo[] FunctionInfo = new IFunctionInfo[]
        {
            new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0),
            new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, Int32.MaxValue),
            new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0),
            new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, Int32.MaxValue),
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

        private sealed class AlwaysFunction : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                return true;
            }
        }

        private sealed class CancelledFunction : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                return conditionContext.State == PipelineState.Canceling;
            }
        }

        private sealed class FailureFunction : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                if (conditionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                Boolean anyFailed = false;
                foreach (var need in GetNeeds(Parameters, context, conditionContext))
                {
                    if (need == null ||
                        !need.TryGetValue(PipelineTemplateConstants.Result, out var resultData) ||
                        !(resultData is StringContextData resultString))
                    {
                        return false;
                    }

                    if (String.Equals(resultString, PipelineTemplateConstants.Failure, StringComparison.OrdinalIgnoreCase))
                    {
                        anyFailed = true;
                        break;
                    }
                }

                return anyFailed;
            }
        }

        private sealed class SuccessFunction : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                var conditionContext = context.State as GraphExecutionContext<TInstance>;
                if (conditionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                Boolean allSucceeded = true;
                foreach (var need in GetNeeds(Parameters, context, conditionContext))
                {
                    if (!allSucceeded ||
                        need == null ||
                        !need.TryGetValue(PipelineTemplateConstants.Result, out var resultData) ||
                        !(resultData is StringContextData resultString) ||
                        !String.Equals(resultString, PipelineTemplateConstants.Success, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
