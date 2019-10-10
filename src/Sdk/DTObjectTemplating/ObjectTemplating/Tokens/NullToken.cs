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
            Int32? column)
            : base(TokenType.Null, fileId, line, column)
        {
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
           return omitSource ? new NullToken(null, null, null) : new NullToken(FileId, Line, Column);
        }

        public override String ToString()
        {
           return String.Empty;
        }
    }
}
