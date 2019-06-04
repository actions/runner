using System;
using System.ComponentModel;
using GitHub.DistributedTask.Pipelines.Runtime;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhaseCondition : GraphCondition<PhaseInstance>
    {
        public PhaseCondition(String condition)
            : base(condition)
        {
        }

        public ConditionResult Evaluate(PhaseExecutionContext context)
        {
            var traceWriter = new ConditionTraceWriter();
            var result = m_parsedCondition.Evaluate<Boolean>(traceWriter, context.SecretMasker, context, context.ExpressionOptions);
            return new ConditionResult() { Value = result, Trace = traceWriter.Trace };
        }
    }
}
