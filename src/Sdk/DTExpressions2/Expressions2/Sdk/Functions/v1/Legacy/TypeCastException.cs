using System;
using GitHub.DistributedTask.Logging;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1.Legacy
{
    internal sealed class TypeCastException : ExpressionException
    {
        internal TypeCastException(Type fromType, Type toType)
            : base(null, String.Empty)
        {
            FromType = fromType;
            ToType = toType;
            m_message = ExpressionResources.TypeCastErrorNoValue(fromType.Name, toType.Name);
        }

        internal TypeCastException(ISecretMasker secretMasker, Object value, ValueKind fromKind, ValueKind toKind)
            : base(null, String.Empty)
        {
            Value = value;
            FromKind = fromKind;
            ToKind = toKind;
            m_message = ExpressionResources.TypeCastError(
                fromKind, // from kind
                toKind, // to kind
                ExpressionUtil.FormatValue(secretMasker, value, fromKind)); // value
        }

        internal TypeCastException(ISecretMasker secretMasker, Object value, ValueKind fromKind, Type toType)
            : base(null, String.Empty)
        {
            Value = value;
            FromKind = fromKind;
            ToType = toType;
            m_message = ExpressionResources.TypeCastError(
                fromKind, // from kind
                toType, // to type
                ExpressionUtil.FormatValue(secretMasker, value, fromKind)); // value
        }

        internal TypeCastException(ISecretMasker secretMasker, Object value, ValueKind fromKind, Type toType, String error)
            : base(null, String.Empty)
        {
            Value = value;
            FromKind = fromKind;
            ToType = toType;
            m_message = ExpressionResources.TypeCastErrorWithError(
                fromKind, // from kind
                toType, // to type
                ExpressionUtil.FormatValue(secretMasker, value, fromKind), // value
                secretMasker != null ? secretMasker.MaskSecrets(error) : error); // error
        }

        public override String Message => m_message;

        internal Object Value { get; }

        internal ValueKind? FromKind { get; }

        internal Type FromType { get; }

        internal ValueKind? ToKind { get; }

        internal Type ToType { get; }

        private readonly String m_message;
    }
}
