using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class StartsWith : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            String left = Parameters[0].EvaluateString(context) ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) ?? String.Empty;
            return left.StartsWith(right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
