using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationPlanReference
    {
        [DataMember]
        public Guid ScopeIdentifier
        {
            get;
            set;
        }

        [DataMember]
        public String PlanType
        {
            get;
            set;
        }

        [DataMember]
        public Int32 Version
        {
            get;
            set;
        }

        [DataMember]
        public Guid PlanId
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
        public Uri ArtifactUri
        {
            get;
            set;
        }

        [DataMember]
        public Uri ArtifactLocation
        {
            get;
            set;
        }

        [IgnoreDataMember]
        internal Int64 ContainerId
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
