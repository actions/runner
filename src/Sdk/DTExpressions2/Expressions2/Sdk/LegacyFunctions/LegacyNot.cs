using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.LegacyFunctions
{
    internal sealed class LegacyNot : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var result = Parameters[0].Evaluate(context);
            return result.IsFalsy;
        }
    }
}
