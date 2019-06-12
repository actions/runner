using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ConditionResult
    {
        [DataMember]
        public Boolean Value
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Trace
        {
            get;
            set;
        }
    }
}
