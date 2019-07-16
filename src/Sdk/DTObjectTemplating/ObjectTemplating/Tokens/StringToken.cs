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
    public sealed class StringToken : LiteralToken, IString
    {
        public StringToken(
            Int32? fileId,
            Int32? line,
            Int32? column,
            String value)
            : base(TokenType.String, fileId, line, column)
        {
            m_value = value;
        }

        public String Value
        {
           get
           {
               if (m_value == null)
               {
                   m_value = String.Empty;
               }

               return m_value;
           }
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
           return omitSource ? new StringToken(null, null, null, m_value) : new StringToken(FileId, Line, Column, m_value);
        }

        public override String ToString()
        {
           return m_value ?? String.Empty;
        }

        String IString.GetString()
        {
            return Value;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_value?.Length == 0)
            {
                m_value = null;
            }
        }

        [DataMember(Name = "lit", EmitDefaultValue = false)]
        private String m_value;
    }
}
