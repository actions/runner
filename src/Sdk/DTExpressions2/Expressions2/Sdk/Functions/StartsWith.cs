using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class StartsWith : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var left = Parameters[0].Evaluate(context);
            if (left.IsPrimitive)
            {
                var leftString = left.ConvertToString();

                var right = Parameters[1].Evaluate(context);
                if (right.IsPrimitive)
                {
                    var rightString = right.ConvertToString();
                    return leftString.StartsWith(rightString, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
