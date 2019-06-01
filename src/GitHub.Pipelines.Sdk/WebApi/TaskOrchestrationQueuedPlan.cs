using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationQueuedPlan
    {
        [DataMember]
        public Guid PlanId
        {
            get;
            set;
        }

        [DataMember]
        public Guid ScopeIdentifier
        {
            get;
            set;
        }

        [DataMember]
        public String PlanGroup
        {
            get;
            set;
        }

        [DataMember]
        public Int32 QueuePosition
        {
            get;
            set;
        }

        [DataMember]
        public Int32 PoolId
        {
            get;
            set;
        }

        [DataMember]
        public DateTime QueueTime
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? AssignTime
        {
            get;
            set;
        }

        [DataMember]
        public TaskOrchestrationOwner Definition
        {
            get;
            set;
        }

        [DataMember]
        public TaskOrchestrationOwner Owner
        {
            get;
            set;
        }
    }
}
