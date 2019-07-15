using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Wildcard : ExpressionNode
    {
        // Prevent the value from being stored on the evaluation context.
        // This avoids unneccessarily duplicating the value in memory.
        protected sealed override Boolean TraceFullyRealized => false;

        internal sealed override String ConvertToExpression()
        {
            return ExpressionConstants.Wildcard.ToString();
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            return ExpressionConstants.Wildcard.ToString();
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            return ExpressionConstants.Wildcard.ToString();
        }
    }

}
