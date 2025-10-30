#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.Expressions.Sdk.Functions
{
    internal sealed class EndsWith : Function
    {
        protected sealed override Boolean TraceFullyExpanded => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var left = Parameters[0].Evaluate(context);
            if (left.IsPrimitive)
            {
                var leftString = left.ConvertToString();

                var right = Parameters[1].Evaluate(context);
                if (right.IsPrimitive)
                {
                    var rightString = right.ConvertToString();
                    return leftString.EndsWith(rightString, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
