using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class NumberToken : LiteralToken, INumber
    {
        public NumberToken(
            Int32? fileId,
            Int32? line,
            Int32? column,
            Double value)
            : base(TokenType.Number, fileId, line, column)
        {
            m_value = value;
        }

        public Double Value => m_value;

        public override TemplateToken Clone(Boolean omitSource)
        {
           return omitSource ? new NumberToken(null, null, null, m_value) : new NumberToken(FileId, Line, Column, m_value);
        }

        public override String ToString()
        {
            return m_value.ToString("G15", CultureInfo.InvariantCulture);
        }

        Double INumber.GetNumber()
        {
            return Value;
        }

        [DataMember(Name = "num", EmitDefaultValue = false)]
        private Double m_value;
    }
}
