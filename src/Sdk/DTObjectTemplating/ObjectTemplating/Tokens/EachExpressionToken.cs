using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class EachExpressionToken : ExpressionToken
    {
        internal EachExpressionToken (
            Int32? fileId,
            Int32? line,
            Int32? column,
            string variable,
            string collection)
            : base(TokenType.EachExpression, fileId, line, column, "each")
        {
            Variable = variable;
            Collection = collection;
        }

        public string Variable { get; private set; }
        public string Collection { get; private set; }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new EachExpressionToken(null, null, null, Variable, Collection) : new EachExpressionToken(FileId, Line, Column, Variable, Collection);
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} {Directive} {Variable} in {Collection} {TemplateConstants.CloseExpression}";
        }
    }
}
