using System;

namespace GitHub.DistributedTask.Expressions
{
    internal sealed class StartsWithNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            String left = Parameters[0].EvaluateString(context) ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) ?? String.Empty;
            return left.StartsWith(right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
