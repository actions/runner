using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.Common.Contracts
{
    [DataContract]
    public class TaskInputValidation : BaseSecuredObject
    {
        public TaskInputValidation()
        {
        }

        private TaskInputValidation(TaskInputValidation toClone, ISecuredObject securedObject)
            : base(securedObject)
        {
            if (toClone != null)
            {
                this.Expression = toClone.Expression;
                this.Message = toClone.Message;
            }
        }

        /// <summary>
        /// Conditional expression
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Expression
        {
            get;
            set;
        }

        /// <summary>
        /// Message explaining how user can correct if validation fails
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Message
        {
            get;
            set;
        }

        public override int GetHashCode()
        {
            return Expression.GetHashCode() ^ Message.GetHashCode();
        }

        public TaskInputValidation Clone()
        {
            return this.Clone(null);
        }

        public TaskInputValidation Clone(ISecuredObject securedObject)
        {
            return new TaskInputValidation(this, securedObject);
        }
    }
}
