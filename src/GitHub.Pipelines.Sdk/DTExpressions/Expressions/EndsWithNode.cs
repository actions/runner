using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class EndsWithNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            String left = Parameters[0].EvaluateString(context) ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) ?? String.Empty;
            return left.EndsWith(right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
