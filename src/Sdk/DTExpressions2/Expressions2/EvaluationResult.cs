using System;
using System.ComponentModel;
using System.Globalization;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class EvaluationResult
    {
        internal EvaluationResult(
            EvaluationContext context,
            Int32 level,
            Object val,
            ValueKind kind,
            Object raw)
            : this(context, level, val, kind, raw, false)
        {
        }

        internal EvaluationResult(
            EvaluationContext context,
            Int32 level,
            Object val,
            ValueKind kind,
            Object raw,
            Boolean omitTracing)
        {
            m_level = level;
            Value = val;
            Kind = kind;
            Raw = raw;
            m_omitTracing = omitTracing;

            if (!omitTracing)
            {
                TraceValue(context);
            }
        }

        public ValueKind Kind { get; }

        /// <summary>
        /// When an interface converter is applied to the node result, raw contains the original value
        /// </summary>
        public Object Raw { get; }

        public Object Value { get; }

        public Boolean IsFalsy
        {
            get
            {
                switch (Kind)
                {
                    case ValueKind.Null:
                        return true;
                    case ValueKind.Boolean:
                        var boolean = (Boolean)Value;
                        return !boolean;
                    case ValueKind.Number:
                        var number = (Double)Value;
                        return number == 0d || Double.IsNaN(number);
                    case ValueKind.String:
                        var str = (String)Value;
                        return String.Equals(str, String.Empty, StringComparison.Ordinal);
                    default:
                        return false;
                }
            }
        }

        public Boolean IsPrimitive => ExpressionUtility.IsPrimitive(Kind);

        public Boolean IsTruthy => !IsFalsy;

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        public Boolean AbstractEqual(EvaluationResult right)
        {
            return AbstractEqual(Value, right.Value);
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        public Boolean AbstractGreaterThan(EvaluationResult right)
        {
            return AbstractGreaterThan(Value, right.Value);
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        public Boolean AbstractGreaterThanOrEqual(EvaluationResult right)
        {
            return AbstractEqual(Value, right.Value) || AbstractGreaterThan(Value, right.Value);
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        public Boolean AbstractLessThan(EvaluationResult right)
        {
            return AbstractLessThan(Value, right.Value);
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        public Boolean AbstractLessThanOrEqual(EvaluationResult right)
        {
            return AbstractEqual(Value, right.Value) || AbstractLessThan(Value, right.Value);
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        public Boolean AbstractNotEqual(EvaluationResult right)
        {
            return !AbstractEqual(Value, right.Value);
        }

        public Double ConvertToNumber()
        {
            return ConvertToNumber(Value);
        }

        public String ConvertToString()
        {
            switch (Kind)
            {
                case ValueKind.Null:
                    return String.Empty;

                case ValueKind.Boolean:
                    return ((Boolean)Value) ? ExpressionConstants.True : ExpressionConstants.False;

                case ValueKind.Number:
                    return ((Double)Value).ToString(ExpressionConstants.NumberFormat, CultureInfo.InvariantCulture);

                case ValueKind.String:
                    return Value as String;

                default:
                    return Kind.ToString();
            }
        }

        public Boolean TryGetCollectionInterface(out Object collection)
        {
            if ((Kind == ValueKind.Object || Kind == ValueKind.Array))
            {
                var obj = Value;
                if (obj is IReadOnlyObject)
                {
                    collection = obj;
                    return true;
                }
                else if (obj is IReadOnlyArray)
                {
                    collection = obj;
                    return true;
                }
            }

            collection = null;
            return false;
        }

        /// <summary>
        /// Useful for working with values that are not the direct evaluation result of a parameter.
        /// This allows ExpressionNode authors to leverage the coercion and comparision functions
        /// for any values.
        ///
        /// Also note, the value will be canonicalized (for example numeric types converted to double) and any
        /// matching interfaces applied.
        /// </summary>
        public static EvaluationResult CreateIntermediateResult(
            EvaluationContext context,
            Object obj)
        {
            var val = ExpressionUtility.ConvertToCanonicalValue(obj, out ValueKind kind, out Object raw);
            return new EvaluationResult(context, 0, val, kind, raw, omitTracing: true);
        }

        private void TraceValue(EvaluationContext context)
        {
            if (!m_omitTracing)
            {
                TraceValue(context, Value, Kind);
            }
        }

        private void TraceValue(
            EvaluationContext context,
            Object val,
            ValueKind kind)
        {
            if (!m_omitTracing)
            {
                TraceVerbose(context, String.Concat("=> ", ExpressionUtility.FormatValue(context?.SecretMasker, val, kind)));
            }
        }

        private void TraceVerbose(
            EvaluationContext context,
            String message)
        {
            if (!m_omitTracing)
            {
                context?.Trace.Verbose(String.Empty.PadLeft(m_level * 2, '.') + (message ?? String.Empty));
            }
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        private static Boolean AbstractEqual(
            Object canonicalLeftValue,
            Object canonicalRightValue)
        {
            CoerceTypes(ref canonicalLeftValue, ref canonicalRightValue, out var leftKind, out var rightKind);

            // Same kind
            if (leftKind == rightKind)
            {
                switch (leftKind)
                {
                    // Null, Null
                    case ValueKind.Null:
                        return true;

                    // Number, Number
                    case ValueKind.Number:
                        var leftDouble = (Double)canonicalLeftValue;
                        var rightDouble = (Double)canonicalRightValue;
                        if (Double.IsNaN(leftDouble) || Double.IsNaN(rightDouble))
                        {
                            return false;
                        }
                        return leftDouble == rightDouble;

                    // String, String
                    case ValueKind.String:
                        var leftString = (String)canonicalLeftValue;
                        var rightString = (String)canonicalRightValue;
                        return String.Equals(leftString, rightString, StringComparison.OrdinalIgnoreCase);

                    // Boolean, Boolean
                    case ValueKind.Boolean:
                        var leftBoolean = (Boolean)canonicalLeftValue;
                        var rightBoolean = (Boolean)canonicalRightValue;
                        return leftBoolean == rightBoolean;

                    // Object, Object
                    case ValueKind.Object:
                    case ValueKind.Array:
                        return Object.ReferenceEquals(canonicalLeftValue, canonicalRightValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        private static Boolean AbstractGreaterThan(
            Object canonicalLeftValue,
            Object canonicalRightValue)
        {
            CoerceTypes(ref canonicalLeftValue, ref canonicalRightValue, out var leftKind, out var rightKind);

            // Same kind
            if (leftKind == rightKind)
            {
                switch (leftKind)
                {
                    // Number, Number
                    case ValueKind.Number:
                        var leftDouble = (Double)canonicalLeftValue;
                        var rightDouble = (Double)canonicalRightValue;
                        if (Double.IsNaN(leftDouble) || Double.IsNaN(rightDouble))
                        {
                            return false;
                        }
                        return leftDouble > rightDouble;

                    // String, String
                    case ValueKind.String:
                        var leftString = (String)canonicalLeftValue;
                        var rightString = (String)canonicalRightValue;
                        return String.Compare(leftString, rightString, StringComparison.OrdinalIgnoreCase) > 0;

                    // Boolean, Boolean
                    case ValueKind.Boolean:
                        var leftBoolean = (Boolean)canonicalLeftValue;
                        var rightBoolean = (Boolean)canonicalRightValue;
                        return leftBoolean && !rightBoolean;
                }
            }

            return false;
        }

        /// <summary>
        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except string comparison is OrdinalIgnoreCase, and objects are not coerced to primitives.
        /// </summary>
        private static Boolean AbstractLessThan(
            Object canonicalLeftValue,
            Object canonicalRightValue)
        {
            CoerceTypes(ref canonicalLeftValue, ref canonicalRightValue, out var leftKind, out var rightKind);

            // Same kind
            if (leftKind == rightKind)
            {
                switch (leftKind)
                {
                    // Number, Number
                    case ValueKind.Number:
                        var leftDouble = (Double)canonicalLeftValue;
                        var rightDouble = (Double)canonicalRightValue;
                        if (Double.IsNaN(leftDouble) || Double.IsNaN(rightDouble))
                        {
                            return false;
                        }
                        return leftDouble < rightDouble;

                    // String, String
                    case ValueKind.String:
                        var leftString = (String)canonicalLeftValue;
                        var rightString = (String)canonicalRightValue;
                        return String.Compare(leftString, rightString, StringComparison.OrdinalIgnoreCase) < 0;

                    // Boolean, Boolean
                    case ValueKind.Boolean:
                        var leftBoolean = (Boolean)canonicalLeftValue;
                        var rightBoolean = (Boolean)canonicalRightValue;
                        return !leftBoolean && rightBoolean;
                }
            }

            return false;
        }

        /// Similar to the Javascript abstract equality comparison algorithm http://www.ecma-international.org/ecma-262/5.1/#sec-11.9.3.
        /// Except objects are not coerced to primitives.
        private static void CoerceTypes(
            ref Object canonicalLeftValue,
            ref Object canonicalRightValue,
            out ValueKind leftKind,
            out ValueKind rightKind)
        {
            leftKind = GetKind(canonicalLeftValue);
            rightKind = GetKind(canonicalRightValue);

            // Same kind
            if (leftKind == rightKind)
            {
            }
            // Number, String
            else if (leftKind == ValueKind.Number && rightKind == ValueKind.String)
            {
                canonicalRightValue = ConvertToNumber(canonicalRightValue);
                rightKind = ValueKind.Number;
            }
            // String, Number
            else if (leftKind == ValueKind.String && rightKind == ValueKind.Number)
            {
                canonicalLeftValue = ConvertToNumber(canonicalLeftValue);
                leftKind = ValueKind.Number;
            }
            // Boolean|Null, Any
            else if (leftKind == ValueKind.Boolean || leftKind == ValueKind.Null)
            {
                canonicalLeftValue = ConvertToNumber(canonicalLeftValue);
                CoerceTypes(ref canonicalLeftValue, ref canonicalRightValue, out leftKind, out rightKind);
            }
            // Any, Boolean|Null
            else if (rightKind == ValueKind.Boolean || rightKind == ValueKind.Null)
            {
                canonicalRightValue = ConvertToNumber(canonicalRightValue);
                CoerceTypes(ref canonicalLeftValue, ref canonicalRightValue, out leftKind, out rightKind);
            }
        }

        /// <summary>
        /// For primitives, follows the Javascript rules (the Number function in Javascript). Otherwise NaN.
        /// </summary>
        private static Double ConvertToNumber(Object canonicalValue)
        {
            var kind = GetKind(canonicalValue);
            switch (kind)
            {
                case ValueKind.Null:
                    return 0d;
                case ValueKind.Boolean:
                    return (Boolean)canonicalValue ? 1d : 0d;
                case ValueKind.Number:
                    return (Double)canonicalValue;
                case ValueKind.String:
                    return ExpressionUtility.ParseNumber(canonicalValue as String);
            }

            return Double.NaN;
        }

        private static ValueKind GetKind(Object canonicalValue)
        {
            if (Object.ReferenceEquals(canonicalValue, null))
            {
                return ValueKind.Null;
            }
            else if (canonicalValue is Boolean)
            {
                return ValueKind.Boolean;
            }
            else if (canonicalValue is Double)
            {
                return ValueKind.Number;
            }
            else if (canonicalValue is String)
            {
                return ValueKind.String;
            }
            else if (canonicalValue is IReadOnlyObject)
            {
                return ValueKind.Object;
            }
            else if (canonicalValue is IReadOnlyArray)
            {
                return ValueKind.Array;
            }

            return ValueKind.Object;
        }

        private readonly Int32 m_level;
        private readonly Boolean m_omitTracing;
    }
}
