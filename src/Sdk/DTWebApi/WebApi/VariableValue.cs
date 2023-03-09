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

        public VariableValue(String value, bool isSecret = false, bool isReadonly = false)
        {
            Value = value;
            IsSecret = isSecret;
            IsReadonly = isReadonly;
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

        [DataMember(EmitDefaultValue = false)]
        public Boolean IsReadonly
        {
            get;
            set;
        }

        // MetaInfo for reserializing group references
        [IgnoreDataMember]
        public Boolean IsGroup
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Boolean IsGroupMember
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
