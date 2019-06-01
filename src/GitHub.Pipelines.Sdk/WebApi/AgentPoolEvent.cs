using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class AgentPoolEvent
    {
        public AgentPoolEvent(
            String eventType, 
            TaskAgentPool pool)
        {
            this.EventType = eventType;
            this.Pool = pool;
        }

        [DataMember]
        public String EventType
        {
            get;
            set;
        }

        [DataMember]
        public TaskAgentPool Pool
        {
            get;
            set;
        }
    }
}
