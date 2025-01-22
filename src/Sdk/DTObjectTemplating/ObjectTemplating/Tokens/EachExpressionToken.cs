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

        [DataMember(Name = "variable", EmitDefaultValue = false)]
        public string Variable { get; set; }
        [DataMember(Name = "collection", EmitDefaultValue = false)]
        public string Collection { get; set; }
        [IgnoreDataMember]
        public TemplateValidationErrors Errors { get; set; }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new EachExpressionToken(null, null, null, Variable, Collection) { Errors = Errors } : new EachExpressionToken(FileId, Line, Column, Variable, Collection) { Errors = Errors };
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} {Directive} {Variable} in {Collection} {TemplateConstants.CloseExpression}";
        }
    }
}
