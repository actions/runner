using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LiteralValueNode : ExpressionNode
    {
        public LiteralValueNode(Object val)
        {
            ValueKind kind;

            // Note, it is OK to pass null EvaluationOptions here since the parser does not support
            // localized values. For example, if parsing local date-times were supported, then we
            // would need to know the account's time zone at parse time. This is an OK limitation,
            // since we can defer this type of problem to runtime, for example by adding a parseDate function.
            Value = ExpressionUtil.ConvertToCanonicalValue(null, val, out kind, out _, out _);

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
            return ExpressionUtil.FormatValue(null, Value, Kind);
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            return ExpressionUtil.FormatValue(null, Value, Kind);
        }

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Value;
        }
    }

}
