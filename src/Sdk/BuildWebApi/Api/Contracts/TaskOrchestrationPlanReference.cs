using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to an orchestration plan.
    /// </summary>
    [DataContract]
    public class TaskOrchestrationPlanReference : BaseSecuredObject
    {
        public TaskOrchestrationPlanReference()
        {
            OrchestrationType = BuildOrchestrationType.Build;
        }

        internal TaskOrchestrationPlanReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
            OrchestrationType = BuildOrchestrationType.Build;
        }

        /// <summary>
        /// The ID of the plan.
        /// </summary>
        [DataMember]
        public Guid PlanId
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the plan.
        /// </summary>
        /// <remarks>
        /// <see cref="BuildOrchestrationType" /> for supported types.
        /// </remarks>
        [DefaultValue(BuildOrchestrationType.Build)]
        [DataMember(EmitDefaultValue = false)]
        public Int32? OrchestrationType
        {
            get;
            set;
        }
    }
}
