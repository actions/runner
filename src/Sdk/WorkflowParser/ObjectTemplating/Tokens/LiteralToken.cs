using System;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens
{
    [DataContract]
    public abstract class LiteralToken : ScalarToken
    {
        public LiteralToken(
            Int32 tokenType,
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(tokenType, fileId, line, column)
        {
        }
    }
}