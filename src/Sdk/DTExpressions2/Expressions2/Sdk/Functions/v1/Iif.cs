using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Iif : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            bool cond = Parameters[0].EvaluateBoolean(context);
            EvaluationResult then = Parameters[1].Evaluate(context);
            EvaluationResult otherwise = Parameters[2].Evaluate(context);
            return cond ? then.Raw ?? then.Value : otherwise.Raw ?? otherwise.Value;
        }
    }
}
