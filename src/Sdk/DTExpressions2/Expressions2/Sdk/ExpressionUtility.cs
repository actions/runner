using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GitHub.DistributedTask.Logging;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ExpressionUtility
    {
        internal static Object ConvertToCanonicalValue(
            Object val,
            out ValueKind kind,
            out Object raw)
        {
            raw = null;

            if (Object.ReferenceEquals(val, null))
            {
                kind = ValueKind.Null;
                return null;
            }
            else if (val is Boolean)
            {
                kind = ValueKind.Boolean;
                return val;
            }
            else if (val is Double)
            {
                kind = ValueKind.Number;
                return val;
            }
            else if (val is String)
            {
                kind = ValueKind.String;
                return val;
            }
            else if (val is INull n)
            {
                kind = ValueKind.Null;
                raw = val;
                return null;
            }
            else if (val is IBoolean boolean)
            {
                kind = ValueKind.Boolean;
                raw = val;
                return boolean.GetBoolean();
            }
            else if (val is INumber number)
            {
                kind = ValueKind.Number;
                raw = val;
                return number.GetNumber();
            }
            else if (val is IString str)
            {
                kind = ValueKind.String;
                raw = val;
                return str.GetString();
            }
            else if (val is IReadOnlyObject)
            {
                kind = ValueKind.Object;
                return val;
            }
            else if (val is IReadOnlyArray)
            {
                kind = ValueKind.Array;
                return val;
            }
            else if (!val.GetType().GetTypeInfo().IsClass)
            {
                if (val is Decimal || val is Byte || val is SByte || val is Int16 || val is UInt16 || val is Int32 || val is UInt32 || val is Int64 || val is UInt64 || val is Single)
                {
                    kind = ValueKind.Number;
                    return Convert.ToDouble(val);
                }
                else if (val is Enum)
                {
                    var strVal = String.Format(CultureInfo.InvariantCulture, "{0:G}", val);
                    if (Double.TryParse(strVal, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out Double doubleValue))
                    {
                        kind = ValueKind.Number;
                        return doubleValue;
                    }

                    kind = ValueKind.String;
                    return strVal;
                }
            }

            kind = ValueKind.Object;
            return val;
        }

        internal static String FormatValue(
            ISecretMasker secretMasker,
            EvaluationResult evaluationResult)
        {
            return FormatValue(secretMasker, evaluationResult.Value, evaluationResult.Kind);
        }

        internal static String FormatValue(
            ISecretMasker secretMasker,
            Object value,
            ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Null:
                    return ExpressionConstants.Null;

                case ValueKind.Boolean:
                    return ((Boolean)value) ? ExpressionConstants.True : ExpressionConstants.False;

                case ValueKind.Number:
                    var strNumber = ((Double)value).ToString(ExpressionConstants.NumberFormat, CultureInfo.InvariantCulture);
                    return secretMasker != null ? secretMasker.MaskSecrets(strNumber) : strNumber;

                case ValueKind.String:
                    // Mask secrets before string-escaping.
                    var strValue = secretMasker != null ? secretMasker.MaskSecrets(value as String) : value as String;
                    return $"'{StringEscape(strValue)}'";

                case ValueKind.Array:
                case ValueKind.Object:
                    return kind.ToString();

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to convert to realized expression. Unexpected value kind: {kind}");
            }
        }

        internal static bool IsLegalKeyword(String str)
        {
            if (String.IsNullOrEmpty(str))
            {
                return false;
            }

            var first = str[0];
            if ((first >= 'a' && first <= 'z') ||
                (first >= 'A' && first <= 'Z') ||
                first == '_')
            {
                for (var i = 1; i < str.Length; i++)
                {
                    var c = str[i];
                    if ((c >= 'a' && c <= 'z') ||
                        (c >= 'A' && c <= 'Z') ||
                        (c >= '0' && c <= '9') ||
                        c == '_' ||
                        c == '-')
                    {
                        // OK
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }

        }

        internal static Boolean IsPrimitive(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Null:
                case ValueKind.Boolean:
                case ValueKind.Number:
                case ValueKind.String:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The rules here attempt to follow Javascript rules for coercing a string into a number
        /// for comparison. That is, the Number() function in Javascript.
        /// </summary>
        internal static Double ParseNumber(String str)
        {
            // Trim
            str = str?.Trim() ?? String.Empty;

            // Empty
            if (String.IsNullOrEmpty(str))
            {
                return 0d;
            }
            // Try parse
            else if (Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            // Check for 0x[0-9a-fA-F]+
            else if (str[0] == '0' &&
                str.Length > 2 &&
                str[1] == 'x' &&
                str.Skip(2).All(x => (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f') || (x >= 'A' && x <= 'F')))
            {
                // Try parse
                if (Int32.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var integer))
                {
                    return (Double)integer;
                }

                // Otherwise exceeds range
            }
            // Check for 0o[0-9]+
            else if (str[0] == '0' &&
                str.Length > 2 &&
                str[1] == 'o' &&
                str.Skip(2).All(x => x >= '0' && x <= '7'))
            {
                // Try parse
                var integer = default(Int32?);
                try
                {
                    integer = Convert.ToInt32(str.Substring(2), 8);
                }
                // Otherwise exceeds range
                catch (Exception)
                {
                }

                // Success
                if (integer != null)
                {
                    return (Double)integer.Value;
                }
            }
            // Infinity
            else if (String.Equals(str, ExpressionConstants.Infinity, StringComparison.Ordinal))
            {
                return Double.PositiveInfinity;
            }
            // -Infinity
            else if (String.Equals(str, ExpressionConstants.NegativeInfinity, StringComparison.Ordinal))
            {
                return Double.NegativeInfinity;
            }

            // Otherwise NaN
            return Double.NaN;
        }

        internal static String StringEscape(String value)
        {
            return String.IsNullOrEmpty(value) ? String.Empty : value.Replace("'", "''");
        }
    }
}
