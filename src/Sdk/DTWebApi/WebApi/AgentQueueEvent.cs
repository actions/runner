using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class AgentQueueEvent
    {
        public AgentQueueEvent(
            String eventType, 
            TaskAgentQueue queue)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(eventType, "eventType");
            ArgumentUtility.CheckForNull(queue, "queue");

            this.EventType = eventType;
            this.Queue = queue;
        }

        [DataMember]
        public String EventType
        {
            get;
            set;
        }

        [DataMember]
        public TaskAgentQueue Queue
        {
            get;
            set;
        }
    }
}
