#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.Expressions.Sdk
{
    public sealed class Wildcard : ExpressionNode
    {
        // Prevent the value from being stored on the evaluation context.
        // This avoids unnecessarily duplicating the value in memory.
        protected sealed override Boolean TraceFullyExpanded => false;

        public sealed override String ConvertToExpression()
        {
            return ExpressionConstants.Wildcard.ToString();
        }

        internal sealed override String ConvertToExpandedExpression(EvaluationContext context)
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
