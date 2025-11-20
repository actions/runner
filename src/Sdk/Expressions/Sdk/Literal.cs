#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.Expressions.Sdk
{
    public sealed class Literal : ExpressionNode
    {
        public Literal(Object val)
        {
            Value = ExpressionUtility.ConvertToCanonicalValue(val, out var kind, out _);
            Kind = kind;
            Name = kind.ToString();
        }

        public ValueKind Kind { get; }

        public Object Value { get; }

        // Prevent the value from being stored on the evaluation context.
        // This avoids unneccessarily duplicating the value in memory.
        protected sealed override Boolean TraceFullyExpanded => false;

        public sealed override String ConvertToExpression()
        {
            return ExpressionUtility.FormatValue(null, Value, Kind);
        }

        internal sealed override String ConvertToExpandedExpression(EvaluationContext context)
        {
            return ExpressionUtility.FormatValue(null, Value, Kind);
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            return Value;
        }
    }

}
