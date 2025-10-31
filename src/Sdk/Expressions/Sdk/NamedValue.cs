using System;

namespace GitHub.Actions.Expressions.Sdk
{
    public abstract class NamedValue : ExpressionNode
    {
        public sealed override string ConvertToExpression() => Name;

        protected sealed override Boolean TraceFullyExpanded => true;

        internal sealed override String ConvertToExpandedExpression(EvaluationContext context)
        {
            // Check if the result was stored
            if (context.TryGetTraceResult(this, out String result))
            {
                return result;
            }

            return Name;
        }
    }
}
