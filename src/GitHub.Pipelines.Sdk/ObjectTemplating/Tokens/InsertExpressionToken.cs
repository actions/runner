using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class InsertExpressionToken : ExpressionToken
    {
        internal InsertExpressionToken(
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.InsertExpression, fileId, line, column, TemplateConstants.InsertDirective)
        {
        }

        public override TemplateToken Clone()
        {
            return Clone(false);
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new InsertExpressionToken(null, null, null) : new InsertExpressionToken(FileId, Line, Column);
        }

        public override String ToString()
        {
            return $"${{{{ insert }}}}";
        }
    }
}