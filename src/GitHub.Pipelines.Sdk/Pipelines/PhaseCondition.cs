using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
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
