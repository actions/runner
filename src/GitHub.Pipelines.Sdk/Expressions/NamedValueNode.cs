using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class NamedValueNode : ExpressionNode
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
