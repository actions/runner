using System;
using System.Globalization;
using System.Linq;

namespace GitHub.DistributedTask.Expressions2.Sdk.Operators
{
    internal sealed class Or : Container
    {
        protected sealed override Boolean TraceFullyRealized => false;

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "({0})",
                String.Join(" || ", Parameters.Select(x => x.ConvertToExpression())));
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
                "({0})",
                String.Join(" || ", Parameters.Select(x => x.ConvertToRealizedExpression(context))));
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var result = default(EvaluationResult);
            foreach (var parameter in Parameters)
            {
                result = parameter.Evaluate(context);
                if (result.IsTruthy)
                {
                    break;
                }
            }

            return result?.Value;
        }
    }
}
