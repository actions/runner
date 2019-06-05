using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [KnownType(typeof(ExpressionValidationItem))]
    [KnownType(typeof(InputValidationItem))]
    [JsonConverter(typeof(ValidationItemJsonConverter))]
    public class ValidationItem
    {
        protected ValidationItem(String type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Type of validation item
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Value to validate. 
        /// The conditional expression to validate for the input for "expression" type
        /// Eg:eq(variables['Build.SourceBranch'], 'refs/heads/master');eq(value, 'refs/heads/master')
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Value
        {
            get;
            set;
        }

        /// <summary>
        /// Tells whether the current input is valid or not
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean? IsValid
        {
            get;
            set;
        }

        /// <summary>
        /// Reason for input validation failure
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Reason
        {
            get;
            set;
        }
    }
}
