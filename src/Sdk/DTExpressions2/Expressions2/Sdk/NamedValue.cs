using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class NamedValue : ExpressionNode
    {
        internal sealed override string ConvertToExpression() => Name;

        protected sealed override Boolean TraceFullyRealized => true;

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
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
