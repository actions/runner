using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ConditionalExpressionToken : ExpressionToken
    {
        public string Condition { get; }
        internal ConditionalExpressionToken (
            int type,
            Int32? fileId,
            Int32? line,
            Int32? column,
            string directive,
            string condition)
            : base(type, fileId, line, column, directive)
        {
            Condition = condition;
        }
    }
}
