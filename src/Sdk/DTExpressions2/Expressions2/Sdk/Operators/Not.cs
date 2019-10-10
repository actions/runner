using System;
using System.Globalization;

namespace GitHub.DistributedTask.Expressions2.Sdk.Operators
{
    internal sealed class Not : Container
    {
        protected sealed override Boolean TraceFullyRealized => false;

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "!{0}",
                Parameters[0].ConvertToExpression());
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
                "!{0}",
                Parameters[0].ConvertToRealizedExpression(context));
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var result = Parameters[0].Evaluate(context);
            return result.IsFalsy;
        }
    }
}
