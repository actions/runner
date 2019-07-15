using System;
using System.Globalization;

namespace GitHub.DistributedTask.Expressions2.Sdk.Operators
{
    internal sealed class LessThan : Container
    {
        protected sealed override Boolean TraceFullyRealized => false;

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "({0} < {1})",
                Parameters[0].ConvertToExpression(),
                Parameters[1].ConvertToExpression());
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            // Check if the result was stored
            if (context.TryGetTraceResult(this, out String result))
            {
                return result;
            }

            return String.Format(
                CultureInfo.InvariantCulture,
                "({0} < {1})",
                Parameters[0].ConvertToRealizedExpression(context),
                Parameters[1].ConvertToRealizedExpression(context));
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var left = Parameters[0].Evaluate(context);
            var right = Parameters[1].Evaluate(context);
            return left.AbstractLessThan(right);
        }
    }
}
