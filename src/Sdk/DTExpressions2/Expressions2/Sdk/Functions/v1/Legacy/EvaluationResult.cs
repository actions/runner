using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Legacy
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
        /// When a custom converter is applied to the node result, raw contains the original value
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Object Raw { get; }

        public Object Value { get; }

        public int CompareTo(
            EvaluationContext context,
            EvaluationResult right)
        {
            Object leftValue;
            ValueKind leftKind;
            switch (Kind)
            {
                case ValueKind.Boolean:
                case ValueKind.DateTime:
                case ValueKind.Number:
                case ValueKind.String:
                case ValueKind.Version:
                    leftValue = Value;
                    leftKind = Kind;
                    break;
                default:
                    leftValue = ConvertToNumber(context); // Will throw or succeed
                    leftKind = ValueKind.Number;
                    break;
            }

            if (leftKind == ValueKind.Boolean)
            {
                Boolean b = right.ConvertToBoolean(context);
                return ((Boolean)leftValue).CompareTo(b);
            }
            else if (leftKind == ValueKind.DateTime)
            {
                DateTimeOffset d = right.ConvertToDateTime(context);
                return ((DateTimeOffset)leftValue).CompareTo(d);
            }
            else if (leftKind == ValueKind.Number)
            {
                Double d = right.ConvertToNumber(context);
                return ((Double)leftValue).CompareTo(d);
            }
            else if (leftKind == ValueKind.String)
            {
                String s = right.ConvertToString(context);
                return String.Compare(leftValue as String ?? String.Empty, s ?? String.Empty, StringComparison.OrdinalIgnoreCase);
            }
            else //if (leftKind == ValueKind.Version)
            {
                Version v = right.ConvertToVersion(context);
                return (leftValue as Version).CompareTo(v);
            }
        }

        public Boolean ConvertToBoolean(EvaluationContext context)
        {
            Boolean result;
            switch (Kind)
            {
                case ValueKind.Boolean:
                    return (Boolean)Value; // Not converted. Don't trace.

                case ValueKind.Number:
                    result = (Double)Value != 0; // 0 converts to false, otherwise true.
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                case ValueKind.String:
                    result = !String.IsNullOrEmpty(Value as String);
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                case ValueKind.Array:
                case ValueKind.DateTime:
                case ValueKind.Object:
                case ValueKind.Version:
                    result = true;
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                case ValueKind.Null:
                    result = false;
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to convert value to Boolean. Unexpected value kind '{Kind}'.");
            }
        }

        public DateTimeOffset ConvertToDateTime(EvaluationContext context)
        {
            DateTimeOffset result;
            if (TryConvertToDateTime(context, out result))
            {
                return result;
            }

            throw new TypeCastException(context?.SecretMasker, Value, fromKind: Kind, toKind: ValueKind.DateTime);
        }

        public Object ConvertToNull(EvaluationContext context)
        {
            Object result;
            if (TryConvertToNull(context, out result))
            {
                return result;
            }

            throw new TypeCastException(context?.SecretMasker, Value, fromKind: Kind, toKind: ValueKind.Null);
        }

        public Double ConvertToNumber(EvaluationContext context)
        {
            Double result;
            if (TryConvertToNumber(context, out result))
            {
                return result;
            }

            throw new TypeCastException(context?.SecretMasker, Value, fromKind: Kind, toKind: ValueKind.Number);
        }

        public String ConvertToString(EvaluationContext context)
        {
            String result;
            if (TryConvertToString(context, out result))
            {
                return result;
            }

            throw new TypeCastException(context?.SecretMasker, Value, fromKind: Kind, toKind: ValueKind.String);
        }

        public Version ConvertToVersion(EvaluationContext context)
        {
            Version result;
            if (TryConvertToVersion(context, out result))
            {
                return result;
            }

            throw new TypeCastException(context?.SecretMasker, Value, fromKind: Kind, toKind: ValueKind.Version);
        }

        public Boolean Equals(
            EvaluationContext context,
            EvaluationResult right)
        {
            if (Kind == ValueKind.Boolean)
            {
                Boolean b = right.ConvertToBoolean(context);
                return (Boolean)Value == b;
            }
            else if (Kind == ValueKind.DateTime)
            {
                DateTimeOffset d;
                if (right.TryConvertToDateTime(context, out d))
                {
                    return (DateTimeOffset)Value == d;
                }
            }
            else if (Kind == ValueKind.Number)
            {
                Double d;
                if (right.TryConvertToNumber(context, out d))
                {
                    return (Double)Value == d;
                }
            }
            else if (Kind == ValueKind.Version)
            {
                Version v;
                if (right.TryConvertToVersion(context, out v))
                {
                    return (Version)Value == v;
                }
            }
            else if (Kind == ValueKind.String)
            {
                String s;
                if (right.TryConvertToString(context, out s))
                {
                    return String.Equals(
                        Value as String ?? String.Empty,
                        s ?? String.Empty,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            else if (Kind == ValueKind.Array || Kind == ValueKind.Object)
            {
                return Kind == right.Kind && Object.ReferenceEquals(Value, right.Value);
            }
            else if (Kind == ValueKind.Null)
            {
                Object n;
                if (right.TryConvertToNull(context, out n))
                {
                    return true;
                }
            }

            return false;
        }

        public Boolean TryConvertToDateTime(
            EvaluationContext context,
            out DateTimeOffset result)
        {
            switch (Kind)
            {
                case ValueKind.DateTime:
                    result = (DateTimeOffset)Value; // Not converted. Don't trace again.
                    return true;

                case ValueKind.String:
                    if (TryParseDateTime(context?.Options, Value as String, out result))
                    {
                        TraceValue(context, result, ValueKind.DateTime);
                        return true;
                    }

                    TraceCoercionFailed(context, toKind: ValueKind.DateTime);
                    return false;

                case ValueKind.Array:
                case ValueKind.Boolean:
                case ValueKind.Null:
                case ValueKind.Number:
                case ValueKind.Object:
                case ValueKind.Version:
                    result = default;
                    TraceCoercionFailed(context, toKind: ValueKind.DateTime);
                    return false;

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to determine whether value can be converted to Number. Unexpected value kind '{Kind}'.");
            }
        }

        public Boolean TryConvertToNull(
            EvaluationContext context,
            out Object result)
        {
            switch (Kind)
            {
                case ValueKind.Null:
                    result = null; // Not converted. Don't trace again.
                    return true;

                case ValueKind.String:
                    if (String.IsNullOrEmpty(Value as String))
                    {
                        result = null;
                        TraceValue(context, result, ValueKind.Null);
                        return true;
                    }

                    break;
            }

            result = null;
            TraceCoercionFailed(context, toKind: ValueKind.Null);
            return false;
        }

        public Boolean TryConvertToNumber(
            EvaluationContext context,
            out Double result)
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    result = (Boolean)Value ? 1 : 0;
                    TraceValue(context, result, ValueKind.Number);
                    return true;

                case ValueKind.Number:
                    result = (Double)Value; // Not converted. Don't trace again.
                    return true;

                case ValueKind.String:
                    String s = Value as String ?? String.Empty;
                    if (String.IsNullOrEmpty(s))
                    {
                        result = 0;
                        TraceValue(context, result, ValueKind.Number);
                        return true;
                    }

                    if (Double.TryParse(s, s_numberStyles, CultureInfo.InvariantCulture, out result))
                    {
                        TraceValue(context, result, ValueKind.Number);
                        return true;
                    }

                    TraceCoercionFailed(context, toKind: ValueKind.Number);
                    return false;

                case ValueKind.Array:
                case ValueKind.DateTime:
                case ValueKind.Object:
                case ValueKind.Version:
                    result = default(Double);
                    TraceCoercionFailed(context, toKind: ValueKind.Number);
                    return false;

                case ValueKind.Null:
                    result = 0;
                    TraceValue(context, result, ValueKind.Number);
                    return true;

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to determine whether value can be converted to Number. Unexpected value kind '{Kind}'.");
            }
        }

        public Boolean TryConvertToString(
            EvaluationContext context,
            out String result)
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    result = String.Format(CultureInfo.InvariantCulture, "{0}", Value);
                    TraceValue(context, result, ValueKind.String);
                    return true;

                case ValueKind.DateTime:
                    result = ((DateTimeOffset)Value).ToString(ExpressionConstants.DateTimeFormat, CultureInfo.InvariantCulture);
                    TraceValue(context, result, ValueKind.String);
                    return true;

                case ValueKind.Number:
                    result = ((Double)Value).ToString(ExpressionConstants.NumberFormat, CultureInfo.InvariantCulture);
                    TraceValue(context, result, ValueKind.String);
                    return true;

                case ValueKind.String:
                    result = Value as String; // Not converted. Don't trace.
                    return true;

                case ValueKind.Version:
                    result = (Value as Version).ToString();
                    TraceValue(context, result, ValueKind.String);
                    return true;

                case ValueKind.Null:
                    result = String.Empty;
                    TraceValue(context, result, ValueKind.Null);
                    return true;

                case ValueKind.Array:
                case ValueKind.Object:
                    result = null;
                    TraceCoercionFailed(context, toKind: ValueKind.String);
                    return false;

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to convert to String. Unexpected value kind '{Kind}'.");
            }
        }

        public Boolean TryConvertToVersion(
            EvaluationContext context,
            out Version result)
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    result = null;
                    TraceCoercionFailed(context, toKind: ValueKind.Version);
                    return false;

                case ValueKind.Number:
                    if (Version.TryParse(ConvertToString(context), out result))
                    {
                        TraceValue(context, result, ValueKind.Version);
                        return true;
                    }

                    TraceCoercionFailed(context, toKind: ValueKind.Version);
                    return false;

                case ValueKind.String:
                    String s = Value as String ?? String.Empty;
                    if (Version.TryParse(s, out result))
                    {
                        TraceValue(context, result, ValueKind.Version);
                        return true;
                    }

                    TraceCoercionFailed(context, toKind: ValueKind.Version);
                    return false;

                case ValueKind.Version:
                    result = Value as Version; // Not converted. Don't trace again.
                    return true;

                case ValueKind.Array:
                case ValueKind.DateTime:
                case ValueKind.Object:
                case ValueKind.Null:
                    result = null;
                    TraceCoercionFailed(context, toKind: ValueKind.Version);
                    return false;

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to convert to Version. Unexpected value kind '{Kind}'.");
            }
        }

        /// <summary>
        /// Useful for working with values that are not the direct evaluation result of a parameter.
        /// This allows ExpressionNode authors to leverage the coercion and comparision functions
        /// for any values.
        ///
        /// Also note, the value will be canonicalized (for example numeric types converted to Double) and any
        /// matching converters applied.
        /// </summary>
        public static EvaluationResult CreateIntermediateResult(
            EvaluationContext context,
            Object obj,
            out ResultMemory conversionResultMemory)
        {
            var val = ExpressionUtil.ConvertToCanonicalValue(context?.Options, obj, out ValueKind kind, out Object raw, out conversionResultMemory);
            return new EvaluationResult(context, 0, val, kind, raw, omitTracing: true);
        }

        private void TraceCoercionFailed(
            EvaluationContext context,
            ValueKind toKind)
        {
            if (!m_omitTracing)
            {
                TraceVerbose(context, String.Format(CultureInfo.InvariantCulture, "=> Unable to coerce {0} to {1}.", Kind, toKind));
            }
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
                TraceVerbose(context, String.Concat("=> ", ExpressionUtil.FormatValue(context?.SecretMasker, val, kind)));
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

        private static Boolean TryParseDateTime(
            EvaluationOptions options,
            String s,
            out DateTimeOffset result)
        {
            if (String.IsNullOrEmpty(s))
            {
                result = default;
                return false;
            }

            s = s.Trim();
            var i = 0;

            // Year, month, day, hour, min, sec
            if (!ReadInt32(s, 4, 4, ref i, out Int32 year) ||
                !ReadSeparator(s, ref i, new[] { '-', '/' }, out Char dateSeparator) ||
                !ReadInt32(s, 1, 2, ref i, out Int32 month) ||
                !ReadSeparator(s, ref i, dateSeparator) ||
                !ReadInt32(s, 1, 2, ref i, out Int32 day) ||
                !ReadSeparator(s, ref i, ' ', 'T') ||
                !ReadInt32(s, 1, 2, ref i, out Int32 hour) ||
                !ReadSeparator(s, ref i, ':') ||
                !ReadInt32(s, 1, 2, ref i, out Int32 minute) ||
                !ReadSeparator(s, ref i, ':') ||
                !ReadInt32(s, 1, 2, ref i, out Int32 second))
            {
                result = default;
                return false;
            }

            // Fraction of second
            Int32 ticks;
            if (ExpressionUtil.SafeCharAt(s, i) == '.')
            {
                i++;
                if (!ReadDigits(s, 1, 7, ref i, out String digits))
                {
                    result = default;
                    return false;
                }

                if (digits.Length < 7)
                {
                    digits = digits.PadRight(7, '0');
                }

                ticks = Int32.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
            }
            else
            {
                ticks = 0;
            }

            TimeSpan offset;

            // End of string indicates local time zone
            if (i >= s.Length)
            {
                // Determine the offset
                var timeZone = TimeZoneInfo.Local;
                try
                {
                    var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                    offset = timeZone.GetUtcOffset(dateTime);
                }
                catch
                {
                    result = default;
                    return false;
                }
            }
            // Offset, then end of string
            else if (!ReadOffset(s, ref i, out offset) ||
                i < s.Length)
            {
                result = default;
                return false;
            }

            // Construct the DateTimeOffset
            try
            {
                result = new DateTimeOffset(year, month, day, hour, minute, second, offset);
            }
            catch
            {
                result = default;
                return false;
            }

            // Add fraction of second
            if (ticks > 0)
            {
                result = result.AddTicks(ticks);
            }

            return true;
        }

        private static Boolean ReadDigits(
            String str,
            Int32 minLength,
            Int32 maxLength,
            ref Int32 index,
            out String result)
        {
            var startIndex = index;
            while (Char.IsDigit(ExpressionUtil.SafeCharAt(str, index)))
            {
                index++;
            }

            var length = index - startIndex;
            if (length < minLength || length > maxLength)
            {
                result = default;
                return false;
            }

            result = str.Substring(startIndex, length);
            return true;
        }

        private static Boolean ReadInt32(
            String str,
            Int32 minLength,
            Int32 maxLength,
            ref Int32 index,
            out Int32 result)
        {
            if (!ReadDigits(str, minLength, maxLength, ref index, out String digits))
            {
                result = default;
                return false;
            }

            result = Int32.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
            return true;
        }

        private static Boolean ReadSeparator(
            String str,
            ref Int32 index,
            params Char[] allowed)
        {
            return ReadSeparator(str, ref index, allowed, out _);
        }

        private static Boolean ReadSeparator(
            String str,
            ref Int32 index,
            Char[] allowed,
            out Char separator)
        {
            separator = ExpressionUtil.SafeCharAt(str, index++);
            foreach (var a in allowed)
            {
                if (separator == a)
                {
                    return true;
                }
            }

            separator = default;
            return false;
        }

        private static Boolean ReadOffset(
            String str,
            ref Int32 index,
            out TimeSpan offset)
        {
            // Z indicates UTC
            if (ExpressionUtil.SafeCharAt(str, index) == 'Z')
            {
                index++;
                offset = TimeSpan.Zero;
                return true;
            }

            Boolean subtract;

            // Negative
            if (ExpressionUtil.SafeCharAt(str, index) == '-')
            {
                index++;
                subtract = true;
            }
            // Positive
            else if (ExpressionUtil.SafeCharAt(str, index) == '+')
            {
                index++;
                subtract = false;
            }
            // Invalid
            else
            {
                offset = default;
                return false;
            }

            // Hour and minute
            if (!ReadInt32(str, 1, 2, ref index, out Int32 hour) ||
                !ReadSeparator(str, ref index, ':') ||
                !ReadInt32(str, 1, 2, ref index, out Int32 minute))
            {
                offset = default;
                return false;
            }

            // Construct the offset
            if (subtract)
            {
                offset = TimeSpan.Zero.Subtract(new TimeSpan(hour, minute, 0));
            }
            else
            {
                offset = new TimeSpan(hour, minute, 0);
            }

            return true;
        }

        private static readonly NumberStyles s_numberStyles =
           NumberStyles.AllowDecimalPoint |
           NumberStyles.AllowLeadingSign |
           NumberStyles.AllowLeadingWhite |
           NumberStyles.AllowThousands |
           NumberStyles.AllowTrailingWhite;
        private static readonly Lazy<JsonSerializer> s_serializer = new Lazy<JsonSerializer>(() => JsonUtility.CreateJsonSerializer());
        private readonly Int32 m_level;
        private readonly Boolean m_omitTracing;
    }
}
