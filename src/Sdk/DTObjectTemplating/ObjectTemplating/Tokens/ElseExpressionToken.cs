using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ElseExpressionToken : ConditionalExpressionToken
    {
        internal ElseExpressionToken (
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.ElseExpression, fileId, line, column, "else", null)
        {
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new ElseExpressionToken(null, null, null) { Errors = Errors } : new ElseExpressionToken(FileId, Line, Column) { Errors = Errors };
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} {Directive} {TemplateConstants.CloseExpression}";
        }
    }
}
