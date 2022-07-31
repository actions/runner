using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class NotIn : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            EvaluationResult left = Parameters[0].Evaluate(context);
            for (Int32 i = 1; i < Parameters.Count; i++)
            {
                EvaluationResult right = Parameters[i].Evaluate(context);
                if (left.Equals(context, right))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
