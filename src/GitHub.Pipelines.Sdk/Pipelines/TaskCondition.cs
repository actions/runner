using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TaskCondition
    {
        public TaskCondition(String condition)
        {
            m_condition = condition ?? Default;
            m_parser = new ExpressionParser();
            m_parsedCondition = m_parser.CreateTree(m_condition, new ConditionTraceWriter(), s_namedValueInfo, s_functionInfo);
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

        public Boolean RequiresVariables
        {
            get
            {
                return m_requiresVariables.Value;
            }
        }

        public ConditionResult Evaluate(JobExecutionContext context)
        {
            var traceWriter = new ConditionTraceWriter();
            var result = m_parsedCondition.Evaluate<Boolean>(traceWriter, context.SecretMasker, context, context.ExpressionOptions);
            return new ConditionResult() { Value = result, Trace = traceWriter.Trace };
        }

        private Boolean HasVariablesReference()
        {
            return m_parsedCondition.GetParameters<VariablesContextNode>().Any();
        }

        private readonly String m_condition;
        private readonly ExpressionParser m_parser;
        private readonly IExpressionNode m_parsedCondition;
        private readonly Lazy<Boolean> m_requiresVariables;

        private static readonly INamedValueInfo[] s_namedValueInfo = new INamedValueInfo[]
        {
            Expressions.ExpressionConstants.VariablesNamedValue,
        };

        private static readonly IFunctionInfo[] s_functionInfo = new IFunctionInfo[]
        {
            new FunctionInfo<AlwaysNode>("always", 0, 0),
            new FunctionInfo<FailedNode>("failed", 0, 0),
            new FunctionInfo<CanceledNode>("canceled", 0, 0),
            new FunctionInfo<SucceededNode>("succeeded", 0, 0),
            new FunctionInfo<SucceededOrFailedNode>("succeededOrFailed", 0, 0),
        };

        private sealed class ConditionTraceWriter : ITraceWriter
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
                var conditionContext = context.State as JobExecutionContext;
                return conditionContext.State == PipelineState.Canceling;
            }
        }

        private sealed class FailedNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                var executionContext = context.State as JobExecutionContext;
                if (executionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                // The variable should always be set into the environment for a job
                if (!executionContext.Variables.TryGetValue(WellKnownDistributedTaskVariables.JobStatus, out var value) || 
                    !Enum.TryParse<TaskResult>(value.Value, true, out var result))
                {
                    return false;
                }

                return result == TaskResult.Failed;
            }
        }

        private sealed class SucceededNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                var executionContext = context.State as JobExecutionContext;
                if (executionContext.State != PipelineState.InProgress)
                {
                    return false;
                }

                // The variable should always be set into the environment for a job
                if (!executionContext.Variables.TryGetValue(WellKnownDistributedTaskVariables.JobStatus, out var value) ||
                    !Enum.TryParse<TaskResult>(value.Value, true, out var result))
                {
                    return false;
                }

                return result == TaskResult.Succeeded || result == TaskResult.SucceededWithIssues;
            }
        }

        private sealed class SucceededOrFailedNode : FunctionNode
        {
            protected override Object EvaluateCore(EvaluationContext context)
            {
                // No reason to look at the status, we just need to know that we're not being cancelled
                var executionContext = context.State as JobExecutionContext;
                return executionContext.State != PipelineState.Canceling;
            }
        }
    }
}
