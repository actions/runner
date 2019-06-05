using System;

namespace GitHub.DistributedTask.Expressions
{
    internal sealed class NotInNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
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
