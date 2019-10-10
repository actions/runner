using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a plan group.
    /// </summary>
    [DataContract]
    public class TaskOrchestrationPlanGroupReference
    {
        /// <summary>
        /// The project ID.
        /// </summary>
        [DataMember]
        public Guid ProjectId
        {
            get; set;
        }

        /// <summary>
        /// The name of the plan group.
        /// </summary>
        [DataMember]
        public String PlanGroup
        {
            get; set;
        }
    }
}
