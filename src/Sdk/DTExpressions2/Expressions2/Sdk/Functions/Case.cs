#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using GitHub.Actions.Expressions.Data;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class Case : Function
    {
        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            // Validate argument count - must be odd (pairs of predicate-result plus default)
            if (Parameters.Count % 2 == 0)
            {
                throw new InvalidOperationException("case requires an odd number of arguments");
            }

            // Evaluate predicate-result pairs
            for (var i = 0; i < Parameters.Count - 1; i += 2)
            {
                var predicate = Parameters[i].Evaluate(context);

                // Predicate must be a boolean
                if (predicate.Kind != ValueKind.Boolean)
                {
                    throw new InvalidOperationException("case predicate must evaluate to a boolean value");
                }

                // If predicate is true, return the corresponding result
                if ((Boolean)predicate.Value)
                {
                    var result = Parameters[i + 1].Evaluate(context);
                    return result.Value;
                }
            }

            // No predicate matched, return default (last argument)
            var defaultResult = Parameters[Parameters.Count - 1].Evaluate(context);
            return defaultResult.Value;
        }
    }
}
