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
    public sealed class NullToken : LiteralToken, INull
    {
        public NullToken(
            Int32? fileId,
            Int32? line,
            Int32? column,
            string rawValue = null)
            : base(TokenType.Null, fileId, line, column)
        {
            m_raw_value = rawValue;
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
           return omitSource ? new NullToken(null, null, null, m_raw_value) : new NullToken(FileId, Line, Column, m_raw_value);
        }

        public override String ToString()
        {
           return m_raw_value ?? String.Empty;
        }

        [IgnoreDataMember]
        private string m_raw_value;
    }
}
