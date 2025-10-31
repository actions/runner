#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Globalization;
using System.Linq;

namespace GitHub.Actions.Expressions.Sdk.Operators
{
    internal sealed class Or : Container
    {
        protected sealed override Boolean TraceFullyExpanded => false;

        public sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "({0})",
                String.Join(" || ", Parameters.Select(x => x.ConvertToExpression())));
        }

        internal sealed override String ConvertToExpandedExpression(EvaluationContext context)
        {
            // Check if the result was stored
            if (context.TryGetTraceResult(this, out String result))
            {
                return result;
            }

            return String.Format(
                CultureInfo.InvariantCulture,
                "({0})",
                String.Join(" || ", Parameters.Select(x => x.ConvertToExpandedExpression(context))));
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
