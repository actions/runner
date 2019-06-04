using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationPlanGroup
    {
        [DataMember]
        public ProjectReference Project
        {
            get;
            internal set;
        }

        [DataMember]
        public String PlanGroup
        {
            get;
            internal set;
        }

        [DataMember]
        public List<TaskAgentJobRequest> RunningRequests
        {
            get
            {
                if (this.m_agentRequests == null)
                {
                    this.m_agentRequests = new List<TaskAgentJobRequest>();
                }

                return this.m_agentRequests;
            }
        }

        private List<TaskAgentJobRequest> m_agentRequests;
    }
}
