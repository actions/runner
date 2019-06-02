using System;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [DataContract]
    public sealed class AgentJobStartedData
    {
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentReference ReservedAgent
        {
            get;
            set;
        }
    }
}
