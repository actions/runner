using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        protected sealed override Boolean TraceFullyRealized => false;

        internal sealed override String ConvertToExpression()
        {
            return ExpressionUtility.FormatValue(null, Value, Kind);
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
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
