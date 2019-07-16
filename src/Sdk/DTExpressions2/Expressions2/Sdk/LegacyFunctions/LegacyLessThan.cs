using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.LegacyFunctions
{
    internal sealed class LegacyLessThan : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var left = Parameters[0].Evaluate(context);
            var right = Parameters[1].Evaluate(context);
            return left.AbstractLessThan(right);
        }
    }
}
