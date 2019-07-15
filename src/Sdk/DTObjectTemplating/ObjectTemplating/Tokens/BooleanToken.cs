using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class BooleanToken : LiteralToken, IBoolean
    {
        public BooleanToken(
            Int32? fileId,
            Int32? line,
            Int32? column,
            Boolean value)
            : base(TokenType.Boolean, fileId, line, column)
        {
            m_value = value;
        }

        public Boolean Value => m_value;

        public override TemplateToken Clone(Boolean omitSource)
        {
           return omitSource ? new BooleanToken(null, null, null, m_value) : new BooleanToken(FileId, Line, Column, m_value);
        }

        public override String ToString()
        {
           return m_value ? "true" : "false";
        }

        Boolean IBoolean.GetBoolean()
        {
            return Value;
        }

        [DataMember(Name = "bool", EmitDefaultValue = false)]
        private Boolean m_value;
    }
}
