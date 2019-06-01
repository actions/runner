using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class EnvironmentDeploymentTarget
    {
        [DataMember]
        public Int32 EnvironmentId { get; set; }

        [DataMember]
        public String EnvironmentName { get; set; }

        [DataMember]
        public EnvironmentResourceReference Resource { get; set; }
    }
}