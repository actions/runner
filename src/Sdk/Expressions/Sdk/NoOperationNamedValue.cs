#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.Expressions.Sdk
{
    /// <summary>
    /// Useful when validating an expression
    /// </summary>
    public sealed class NoOperationNamedValue : NamedValue
    {
        protected override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            return null;
        }
    }
}
