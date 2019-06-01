using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CounterNode : FunctionNode
    {
        protected override Object EvaluateCore(EvaluationContext evaluationContext)
        {
            int seed = 0;
            var prefix = String.Empty;
            if (Parameters.Count > 0)
            {
                prefix = Parameters[0].EvaluateString(evaluationContext);
            }

            if (Parameters.Count > 1)
            {
                seed = Convert.ToInt32(Parameters[1].EvaluateNumber(evaluationContext));
            }

            var context = evaluationContext.State as IPipelineContext;
            return context.CounterStore?.Increment(context, prefix, seed) ?? seed;
        }
    }
}
