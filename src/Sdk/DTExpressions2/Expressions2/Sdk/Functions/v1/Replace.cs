using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Replace : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            String left = Parameters[0].EvaluateString(context) as String ?? String.Empty;
            String r = Parameters[1].EvaluateString(context) as String ?? String.Empty;
            String v = Parameters[2].EvaluateString(context) as String ?? String.Empty;
            return left.Replace(r, v, StringComparison.OrdinalIgnoreCase);
        }
    }
}
