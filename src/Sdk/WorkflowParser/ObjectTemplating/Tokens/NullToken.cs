using System;
using System.Runtime.Serialization;
using GitHub.Actions.Expressions.Sdk;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens
{
    [DataContract]
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