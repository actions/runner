using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class In : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var left = Parameters[0].Evaluate(context);
            for (var i = 1; i < Parameters.Count; i++)
            {
                var right = Parameters[i].Evaluate(context);
                if (left.AbstractEqual(right))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
