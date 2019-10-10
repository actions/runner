using System;
using System.ComponentModel;
using GitHub.DistributedTask.Pipelines.Runtime;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StageCondition : GraphCondition<StageInstance>
    {
        public StageCondition(String condition)
            : base(condition)
        {
        }

        public ConditionResult Evaluate(StageExecutionContext context)
        {
            var traceWriter = new ConditionTraceWriter();
            var evaluationResult = m_parsedCondition.Evaluate(traceWriter, context.SecretMasker, context, context.ExpressionOptions);
            return new ConditionResult() { Value = evaluationResult.IsTruthy, Trace = traceWriter.Trace };
        }
    }
}
