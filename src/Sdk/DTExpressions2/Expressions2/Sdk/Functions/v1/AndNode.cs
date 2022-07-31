using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class And : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            foreach (ExpressionNode parameter in Parameters)
            {
                if (!parameter.EvaluateBoolean(context))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
