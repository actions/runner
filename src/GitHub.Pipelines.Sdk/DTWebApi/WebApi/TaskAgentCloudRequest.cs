using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentCloudRequest
    {
        private TaskAgentCloudRequest(TaskAgentCloudRequest requestToBeCloned)
        {
            this.AgentCloudId = requestToBeCloned.AgentCloudId;
            this.RequestId = requestToBeCloned.RequestId;
            this.AgentSpecification = requestToBeCloned.AgentSpecification;
            this.ProvisionRequestTime = requestToBeCloned.ProvisionRequestTime;
            this.ProvisionedTime = requestToBeCloned.ProvisionedTime;
            this.AgentConnectedTime = requestToBeCloned.AgentConnectedTime;
            this.ReleaseRequestTime = requestToBeCloned.ReleaseRequestTime;

            if (requestToBeCloned.AgentData != null)
            {
                this.AgentData = new JObject(requestToBeCloned.AgentData);
            }

            if (requestToBeCloned.Pool != null)
            {
                this.Pool = requestToBeCloned.Pool.Clone();
            }

            if(requestToBeCloned.Agent != null)
            {
                this.Agent = requestToBeCloned.Agent.Clone();
            }
        }

        public TaskAgentCloudRequest()
        {
        }

        [DataMember]
        public Int32 AgentCloudId
        {
            get;
            set;
        }

        [DataMember]
        public Guid RequestId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskAgentReference Agent
        {
            get;
            set;
        }

        [DataMember]
        public JObject AgentSpecification
        {
            get;
            set;
        }

        [DataMember]
        public JObject AgentData
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? ProvisionRequestTime
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? ProvisionedTime
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? AgentConnectedTime
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? ReleaseRequestTime
        {
            get;
            set;
        }

        public TaskAgentCloudRequest Clone()
        {
            return new TaskAgentCloudRequest(this);
        }
    }
}
