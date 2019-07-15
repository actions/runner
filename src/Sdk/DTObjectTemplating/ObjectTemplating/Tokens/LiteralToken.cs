using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
