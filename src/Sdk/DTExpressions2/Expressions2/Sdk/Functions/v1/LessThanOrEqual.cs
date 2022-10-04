using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class LessThanOrEqual : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) <= 0;
        }
    }
}
