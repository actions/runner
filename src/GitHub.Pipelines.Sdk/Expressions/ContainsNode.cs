using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class ContainsNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            String left = Parameters[0].EvaluateString(context) as String ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) as String ?? String.Empty;
            return left.IndexOf(right, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
