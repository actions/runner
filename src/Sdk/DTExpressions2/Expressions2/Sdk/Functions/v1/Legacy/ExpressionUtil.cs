using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using GitHub.DistributedTask.Logging;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Legacy
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ExpressionUtil
    {
        internal static Object ConvertToCanonicalValue(
            EvaluationOptions options,
            Object val,
            out ValueKind kind,
            out Object raw,
            out ResultMemory conversionResultMemory)
        {
            raw = null;
            conversionResultMemory = null;

            if (Object.ReferenceEquals(val, null))
            {
                kind = ValueKind.Null;
                return null;
            }
            else if (val is IString str)
            {
                kind = ValueKind.String;
                return str.GetString();
            }
            else if (val is IBoolean booleanValue)
            {
                kind = ValueKind.Boolean;
                return booleanValue.GetBoolean();
            }
            else if (val is INumber num)
            {
                kind = ValueKind.Number;
                return num.GetNumber();
            }
            else if (val is JToken)
            {
                var jtoken = val as JToken;
                switch (jtoken.Type)
                {
                    case JTokenType.Array:
                        kind = ValueKind.Array;
                        return jtoken;
                    case JTokenType.Boolean:
                        kind = ValueKind.Boolean;
                        return jtoken.ToObject<Boolean>();
                    case JTokenType.Float:
                    case JTokenType.Integer:
                        kind = ValueKind.Number;
                        // todo: test the extents of the conversion
                        return jtoken.ToObject<Double>();
                    case JTokenType.Null:
                        kind = ValueKind.Null;
                        return null;
                    case JTokenType.Object:
                        kind = ValueKind.Object;
                        return jtoken;
                    case JTokenType.String:
                        kind = ValueKind.String;
                        return jtoken.ToObject<String>();
                }
            }
            else if (val is String)
            {
                kind = ValueKind.String;
                return val;
            }
            else if (val is Version)
            {
                kind = ValueKind.Version;
                return val;
            }
            else if (!val.GetType().GetTypeInfo().IsClass)
            {
                if (val is Boolean)
                {
                    kind = ValueKind.Boolean;
                    return val;
                }
                else if (val is DateTimeOffset)
                {
                    kind = ValueKind.DateTime;
                    return val;
                }
                else if (val is DateTime dateTime)
                {
                    kind = ValueKind.DateTime;
                    switch (dateTime.Kind)
                    {
                        // When Local: convert to preferred time zone
                        case DateTimeKind.Local:
                            var targetTimeZone = TimeZoneInfo.Local;
                            var localDateTimeOffset = new DateTimeOffset(dateTime);
                            return TimeZoneInfo.ConvertTime(localDateTimeOffset, targetTimeZone);
                        // When Unspecified: assume preferred time zone
                        case DateTimeKind.Unspecified:
                            var timeZone = TimeZoneInfo.Local;
                            var offset = timeZone.GetUtcOffset(dateTime);
                            return new DateTimeOffset(dateTime, offset);
                        // When UTC: keep UTC
                        case DateTimeKind.Utc:
                            return new DateTimeOffset(dateTime);
                        default:
                            throw new NotSupportedException($"Unexpected DateTimeKind '{dateTime.Kind}'"); // Should never happen
                    }
                }
                else if (val is Double || val is Byte || val is SByte || val is Int16 || val is UInt16 || val is Int32 || val is UInt32 || val is Int64 || val is UInt64 || val is Single || val is Double)
                {
                    kind = ValueKind.Number;
                    return Convert.ToDouble(val);
                }
                else if (val is Enum)
                {
                    var strVal = String.Format(CultureInfo.InvariantCulture, "{0:G}", val);
                    if (Double.TryParse(strVal, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out Double decVal))
                    {
                        kind = ValueKind.Number;
                        return decVal;
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
                case ValueKind.Boolean:
                    return ((Boolean)value).ToString();

                case ValueKind.DateTime:
                    var strDateTime = "(DateTime)" + ((DateTimeOffset)value).ToString(ExpressionConstants.DateTimeFormat, CultureInfo.InvariantCulture);
                    return secretMasker != null ? secretMasker.MaskSecrets(strDateTime) : strDateTime;

                case ValueKind.Number:
                    var strNumber = ((Double)value).ToString(ExpressionConstants.NumberFormat, CultureInfo.InvariantCulture);
                    return secretMasker != null ? secretMasker.MaskSecrets(strNumber) : strNumber;

                case ValueKind.String:
                    // Mask secrets before string-escaping.
                    var strValue = secretMasker != null ? secretMasker.MaskSecrets(value as String) : value as String;
                    return $"'{StringEscape(strValue)}'";

                case ValueKind.Version:
                    String strVersion = secretMasker != null ? secretMasker.MaskSecrets(value.ToString()) : value.ToString();
                    return $"v{strVersion}";

                case ValueKind.Array:
                case ValueKind.Null:
                case ValueKind.Object:
                    return kind.ToString();

                default: // Should never reach here.
                    throw new NotSupportedException($"Unable to convert to realized expression. Unexpected value kind: {kind}");
            }
        }

        internal static Char SafeCharAt(
            String str,
            Int32 index)
        {
            if (str.Length > index)
            {
                return str[index];
            }

            return '\0';
        }

        internal static String StringEscape(String value)
        {
            return String.IsNullOrEmpty(value) ? String.Empty : value.Replace("'", "''");
        }
    }
}
