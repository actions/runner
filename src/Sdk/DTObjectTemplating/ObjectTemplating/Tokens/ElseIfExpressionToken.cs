using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ElseIfExpressionToken : ConditionalExpressionToken
    {
        internal ElseIfExpressionToken (
            Int32? fileId,
            Int32? line,
            Int32? column,
            string condition)
            : base(TokenType.ElseIfExpression, fileId, line, column, "elseif", condition)
        {
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new ElseIfExpressionToken(null, null, null, Condition) : new ElseIfExpressionToken(FileId, Line, Column, Condition);
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} {Directive} {Condition} {TemplateConstants.CloseExpression}";
        }
    }
}
