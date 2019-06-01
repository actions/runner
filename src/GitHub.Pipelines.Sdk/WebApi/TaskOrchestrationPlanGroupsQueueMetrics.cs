using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationPlanGroupsQueueMetrics
    {
        [DataMember]
        public PlanGroupStatus Status
        {
            get;
            set;
        }

        [DataMember]
        public Int32 Count
        {
            get;
            set;
        }
    }
}
