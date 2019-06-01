using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class VirtualMachineGroup : EnvironmentResource
    {
        [DataMember]
        public Int32 PoolId { get; set; }
    }
}
