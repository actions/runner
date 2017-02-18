// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Expressions
// Repo 2) vsts-agent repo on GitHub under src/Microsoft.VisualStudio.Services.Agent/DistributedTask.Expressions
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
{
    public sealed class EvaluationResult
    {
        // internal EvaluationResult(EvaluationContext context, Int32 level, Object raw)
        // {
        //     m_level = level;
        //     ValueKind kind;
        //     Value = ConvertToCanonicalValue(raw, out kind);
        //     Kind = kind;
        //     TraceValue(context);
        // }

        internal EvaluationResult(EvaluationContext context, Int32 level, Object val, ValueKind kind)
        {
            m_level = level;
            Value = val;
            Kind = kind;
            TraceValue(context);
        }

        public ValueKind Kind { get; }

        public Object Value { get; }

        public int CompareTo(EvaluationContext context, EvaluationResult right)
        {
            Object leftValue;
            ValueKind leftKind;
            switch (Kind)
            {
                case ValueKind.Boolean:
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
            else if (leftKind == ValueKind.Number)
            {
                Decimal d = right.ConvertToNumber(context);
                return ((Decimal)leftValue).CompareTo(d);
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
                    result = (Decimal)Value != 0m; // 0 converts to false, otherwise true.
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                case ValueKind.String:
                    result = !String.IsNullOrEmpty(Value as String);
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                case ValueKind.Array:
                case ValueKind.Object:
                case ValueKind.Version:
                    result = true;
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                case ValueKind.Null:
                    result = false;
                    TraceValue(context, result, ValueKind.Boolean);
                    return result;

                default:
                    throw new NotSupportedException($"Unable to convert value to Boolean. Unexpected value kind '{Kind}'.");
            }
        }

        public Object ConvertToNull(EvaluationContext context)
        {
            Object result;
            if (TryConvertToNull(context, out result))
            {
                return result;
            }

            throw new TypeCastException(Value, fromKind: Kind, toKind: ValueKind.Null);
        }

        public Decimal ConvertToNumber(EvaluationContext context)
        {
            Decimal result;
            if (TryConvertToNumber(context, out result))
            {
                return result;
            }

            throw new TypeCastException(Value, fromKind: Kind, toKind: ValueKind.Number);
        }

        public String ConvertToString(EvaluationContext context)
        {
            String result;
            if (TryConvertToString(context, out result))
            {
                return result;
            }

            throw new TypeCastException(Value, fromKind: Kind, toKind: ValueKind.String);
        }

        public Version ConvertToVersion(EvaluationContext context)
        {
            Version result;
            if (TryConvertToVersion(context, out result))
            {
                return result;
            }

            throw new TypeCastException(Value, fromKind: Kind, toKind: ValueKind.Version);
        }

        public Boolean Equals(EvaluationContext context, EvaluationResult right)
        {
            if (Kind == ValueKind.Boolean)
            {
                Boolean b = right.ConvertToBoolean(context);
                return (Boolean)Value == b;
            }
            else if (Kind == ValueKind.Number)
            {
                Decimal d;
                if (right.TryConvertToNumber(context, out d))
                {
                    return (Decimal)Value == d;
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

        public Boolean TryConvertToNull(EvaluationContext context, out Object result)
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

        public Boolean TryConvertToNumber(EvaluationContext context, out Decimal result)
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    result = (Boolean)Value ? 1m : 0m;
                    TraceValue(context, result, ValueKind.Number);
                    return true;

                case ValueKind.Number:
                    result = (Decimal)Value; // Not converted. Don't trace again.
                    return true;

                case ValueKind.String:
                    String s = Value as String ?? String.Empty;
                    if (String.IsNullOrEmpty(s))
                    {
                        result = 0m;
                        TraceValue(context, result, ValueKind.Number);
                        return true;
                    }

                    if (Decimal.TryParse(s, s_numberStyles, CultureInfo.InvariantCulture, out result))
                    {
                        TraceValue(context, result, ValueKind.Number);
                        return true;
                    }

                    TraceCoercionFailed(context, toKind: ValueKind.Number);
                    return false;

                case ValueKind.Array:
                case ValueKind.Object:
                case ValueKind.Version:
                    result = default(Decimal);
                    TraceCoercionFailed(context, toKind: ValueKind.Number);
                    return false;

                case ValueKind.Null:
                    result = 0m;
                    TraceValue(context, result, ValueKind.Number);
                    return true;

                default:
                    throw new NotSupportedException($"Unable to determine whether value can be converted to Number. Unexpected value kind '{Kind}'.");
            }
        }

        public Boolean TryConvertToString(EvaluationContext context, out String result)
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    result = String.Format(CultureInfo.InvariantCulture, "{0}", Value);
                    TraceValue(context, result, ValueKind.String);
                    return true;

                case ValueKind.Number:
                    result = ((Decimal)Value).ToString("G", CultureInfo.InvariantCulture);
                    if (result.Contains("."))
                    {
                        result = result.TrimEnd('0').TrimEnd('.'); // Omit trailing zeros after the decimal point.
                    }

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

                default:
                    throw new NotSupportedException($"Unable to convert to String. Unexpected value kind '{Kind}'.");
            }
        }

        public bool TryConvertToVersion(EvaluationContext context, out Version result)
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
                case ValueKind.Object:
                case ValueKind.Null:
                    result = null;
                    TraceCoercionFailed(context, toKind: ValueKind.Version);
                    return false;

                default:
                    throw new NotSupportedException($"Unable to convert to Version. Unexpected value kind '{Kind}'.");
            }
        }

        internal string ConvertToRealizedExpression()
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    return ((Boolean)Value).ToString();

                case ValueKind.Number:
                    String str = ((Decimal)Value).ToString("G", CultureInfo.InvariantCulture);
                    if (str.Contains("."))
                    {
                        str = str.TrimEnd('0').TrimEnd('.'); // Omit trailing zeros after the decimal point.
                    }

                    return str;

                case ValueKind.String:
                    return String.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}'",
                        (Value as String ?? String.Empty).Replace("'", "''"));

                case ValueKind.Version:
                    return String.Format(CultureInfo.InvariantCulture, "v{0}", Value);

                case ValueKind.Array:
                case ValueKind.Null:
                case ValueKind.Object:
                    return Kind.ToString();

                default:
                    throw new NotSupportedException($"Unable to convert to realized expression. Unexpected value kind: {Kind}");
            }
        }

        private void TraceCoercionFailed(EvaluationContext context, ValueKind toKind)
        {
            TraceVerbose(context, String.Format(CultureInfo.InvariantCulture, "=> Unable to coerce {0} to {1}.", Kind, toKind));
        }

        private void TraceValue(EvaluationContext context)
        {
            TraceValue(context, Value, Kind);
        }

        private void TraceValue(EvaluationContext context, Object val, ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Boolean:
                case ValueKind.Number:
                case ValueKind.Version:
                    TraceVerbose(context, String.Format(CultureInfo.InvariantCulture, "=> ({0}) {1}", kind, val));
                    break;
                case ValueKind.String:
                    TraceVerbose(context, String.Format(CultureInfo.InvariantCulture, "=> ({0}) '{1}'", kind, (val as String).Replace("'", "''")));
                    break;
                default:
                    TraceVerbose(context, String.Format(CultureInfo.InvariantCulture, "=> ({0})", kind));
                    break;
            }
        }

        private void TraceVerbose(EvaluationContext context, String message)
        {
            context.Trace.Verbose(String.Empty.PadLeft(m_level * 2, '.') + (message ?? String.Empty));
        }

        private static readonly NumberStyles s_numberStyles =
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowLeadingSign |
            NumberStyles.AllowLeadingWhite |
            NumberStyles.AllowThousands |
            NumberStyles.AllowTrailingWhite;
        private readonly Int32 m_level;
    }

    internal sealed class TypeCastException : InvalidCastException
    {
        internal TypeCastException(Object val, ValueKind fromKind, ValueKind toKind)
        {
            Value = val;
            FromKind = fromKind;
            ToKind = toKind;
            switch (fromKind)
            {
                case ValueKind.Boolean:
                case ValueKind.Number:
                case ValueKind.String:
                case ValueKind.Version:
                    // TODO: loc
                    Message = $"Unable to convert from {FromKind} to {ToKind}. Value: '{val}'";
                    break;
                default:
                    // TODO: loc
                    Message = $"Unable to convert from {FromKind} to {ToKind}. Value: '{val}'";
                    break;
            }
        }

        internal Object Value { get; }

        internal ValueKind FromKind { get; }

        internal ValueKind ToKind { get; }

        public sealed override String Message { get; }
    }
}