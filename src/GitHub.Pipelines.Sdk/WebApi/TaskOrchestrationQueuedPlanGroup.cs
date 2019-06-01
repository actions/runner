using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOrchestrationQueuedPlanGroup
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
        public Int32 QueuePosition
        {
            get;
            internal set;
        }

        [DataMember]
        public List<TaskOrchestrationQueuedPlan> Plans
        {
            get
            {
                if (this._plans == null)
                {
                    this._plans = new List<TaskOrchestrationQueuedPlan>();
                }

                return this._plans;
            }
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

        private List<TaskOrchestrationQueuedPlan> _plans;
    }
}
