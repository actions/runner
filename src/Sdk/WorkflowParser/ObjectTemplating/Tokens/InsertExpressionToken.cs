using System;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens
{
    [DataContract]
    public sealed class InsertExpressionToken : ExpressionToken
    {
        public InsertExpressionToken(
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.InsertExpression, fileId, line, column, TemplateConstants.InsertDirective)
        {
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new InsertExpressionToken(null, null, null) : new InsertExpressionToken(FileId, Line, Column);
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} insert {TemplateConstants.CloseExpression}";
        }
    }
}