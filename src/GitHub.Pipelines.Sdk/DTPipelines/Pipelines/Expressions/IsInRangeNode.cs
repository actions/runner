using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class IsInRangeNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        public static Int32 minParameters = 3;
        public static Int32 maxParameters = 3;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            // isInRange(value: string, min: string, max: string) 
            decimal value = Parameters[0].EvaluateNumber(context);
            decimal min = Parameters[1].EvaluateNumber(context);
            decimal max = Parameters[2].EvaluateNumber(context);
            return value >= min && value <= max;
        }
    }
}
