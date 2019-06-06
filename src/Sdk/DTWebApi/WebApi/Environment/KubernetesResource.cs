using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class KubernetesResource : EnvironmentResource
    {
        [DataMember]
        public String Namespace { get; set; }

        [DataMember]
        public String ClusterName { get; set; }

        [DataMember]
        public Guid ServiceEndpointId { get; set; }
    }
}
