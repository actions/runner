using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class VirtualMachineGroupCreateParameters
    {
        [DataMember]
        public String Name { get; set; }
    }
}
