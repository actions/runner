using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class VariableValue
    {
        public VariableValue()
        {
        }

        public VariableValue(VariableValue value)
            : this(value.Value, value.IsSecret)
        {
        }

        public VariableValue(String value, Boolean isSecret)
        {
            Value = value;
            IsSecret = isSecret;
        }

        [DataMember(EmitDefaultValue = true)]
        public String Value
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean IsSecret
        {
            get;
            set;
        }

        public static implicit operator VariableValue(String value)
        {
            return new VariableValue(value, false);
        }
    }
}
