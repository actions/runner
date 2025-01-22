using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class IfExpressionToken : ConditionalExpressionToken
    {
        internal IfExpressionToken (
            Int32? fileId,
            Int32? line,
            Int32? column,
            string condition)
            : base(TokenType.IfExpression, fileId, line, column, "if", condition)
        {
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new IfExpressionToken(null, null, null, Condition) { Errors = Errors } : new IfExpressionToken(FileId, Line, Column, Condition) { Errors = Errors };
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} {Directive} {Condition} {TemplateConstants.CloseExpression}";
        }
    }
}
