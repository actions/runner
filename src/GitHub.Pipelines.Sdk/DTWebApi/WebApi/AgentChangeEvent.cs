using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class AgentChangeEvent
    {
        [JsonConstructor]
        internal AgentChangeEvent()
        {
        }

        [Obsolete]
        public AgentChangeEvent(
           String eventType,
           Int32 poolId,
           TaskAgent agent,
           DateTime timeStamp)
        {
            this.Agent = agent;
            this.EventType = eventType;
            this.PoolId = poolId;
            this.TimeStamp = timeStamp;
        }

        public AgentChangeEvent(
            String eventType,
            TaskAgent agent,
            TaskAgentPoolReference pool)
        {
            this.Agent = agent;
            this.EventType = eventType;
            m_pool = pool;

#pragma warning disable CS0612 // Type or member is obsolete
            this.TimeStamp = DateTime.Now;
#pragma warning restore CS0612 // Type or member is obsolete

            if (pool != null)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                this.PoolId = pool.Id;
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }

        [DataMember]
        public TaskAgent Agent
        {
            get;
            set;
        }

        [DataMember]
        public String EventType
        {
            get;
            set;
        }

        [Obsolete]
        [DataMember]
        public Int32 PoolId
        {
            get;
            set;
        }

        [Obsolete]
        [DataMember]
        public DateTime TimeStamp
        {
            get;
            set;
        }

        public TaskAgentPoolReference Pool
        {
            get
            {
                if(m_pool == null)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    m_pool = new TaskAgentPoolReference(Guid.Empty, PoolId);
#pragma warning restore CS0612 // Type or member is obsolete
                }

                return m_pool;
            }
        }

        [DataMember(Name = "Pool")]
        private TaskAgentPoolReference m_pool;
    }
}
