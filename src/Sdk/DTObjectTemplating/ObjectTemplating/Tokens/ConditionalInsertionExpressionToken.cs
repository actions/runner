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
        [DataMember(Name = "condition", EmitDefaultValue = false)]
        public string Condition { get; set; }
        [IgnoreDataMember]
        public TemplateValidationErrors Errors { get; set; }
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
