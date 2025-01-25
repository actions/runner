using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Iif : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            // TODO learn the semantics of this function
            memory = null;
            bool cond = Parameters[0].EvaluateBoolean(context);
            String body = Parameters[cond ? 1 : 2].EvaluateString(context) as String ?? String.Empty;
            return body;
        }
    }
}
