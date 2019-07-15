using System;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.Runtime
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
