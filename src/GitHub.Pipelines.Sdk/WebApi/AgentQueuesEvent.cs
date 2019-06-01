using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class AgentQueuesEvent
    {
        public AgentQueuesEvent(
            String eventType, 
            IEnumerable<TaskAgentQueue> queues)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(eventType, "eventType");
            ArgumentUtility.CheckEnumerableForNullOrEmpty(queues, "queues");

            this.EventType = eventType;
            this.Queues = new List<TaskAgentQueue>(queues);
        }

        [DataMember]
        public String EventType
        {
            get;
            set;
        }

        [DataMember]
        public IEnumerable<TaskAgentQueue> Queues
        {
            get;
            set;
        }
    }
}
