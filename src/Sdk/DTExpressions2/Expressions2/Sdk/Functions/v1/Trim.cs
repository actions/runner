using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Trim : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            String left = Parameters[0].EvaluateString(context) as String ?? String.Empty;
            return left.Trim();
        }
    }
}
