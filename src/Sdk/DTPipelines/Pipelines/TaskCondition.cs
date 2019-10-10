using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Pipelines.Expressions;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
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
                return "success()";
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
            var evaluationResult = m_parsedCondition.Evaluate(traceWriter, context.SecretMasker, context, context.ExpressionOptions);
            return new ConditionResult() { Value = evaluationResult.IsTruthy, Trace = traceWriter.Trace };
        }

        private Boolean HasVariablesReference()
        {
            return false;
        }

        private readonly String m_condition;
        private readonly ExpressionParser m_parser;
        private readonly IExpressionNode m_parsedCondition;
        private readonly Lazy<Boolean> m_requiresVariables;

        private static readonly INamedValueInfo[] s_namedValueInfo = new INamedValueInfo[]
        {
        };

        private static readonly IFunctionInfo[] s_functionInfo = new IFunctionInfo[]
        {
            new FunctionInfo<AlwaysNode>("always", 0, 0),
            new FunctionInfo<FailureNode>("failure", 0, 0),
            new FunctionInfo<CancelledNode>("cancelled", 0, 0),
            new FunctionInfo<SuccessNode>("success", 0, 0),
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

        private sealed class AlwaysNode : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                return true;
            }
        }

        private sealed class CancelledNode : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                var conditionContext = context.State as JobExecutionContext;
                return conditionContext.State == PipelineState.Canceling;
            }
        }

        private sealed class FailureNode : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
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

        private sealed class SuccessNode : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
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
    }
}
