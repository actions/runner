using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    public class Snapshot
    {
        [DataMember(EmitDefaultValue = false)]
        public String ImageName { get; set; }
    }
}
