using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    public class TaskOrchestrationPlanGroupsStartedEvent
    {
        public TaskOrchestrationPlanGroupsStartedEvent(IList<TaskOrchestrationPlanGroupReference> planGroups)
        {
            this.PlanGroups = planGroups;
        }

        [DataMember(IsRequired = true)]
        public IList<TaskOrchestrationPlanGroupReference> PlanGroups { get; private set; }
    }
}
