using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Represents service endpoint execution data.
    /// </summary>
    [DataContract]
    public sealed class ServiceEndpointExecutionData
    {
        public ServiceEndpointExecutionData()
        {
        }

        public ServiceEndpointExecutionData(
            String planType,
            TaskOrchestrationOwner definition,
            TaskOrchestrationOwner owner,
            DateTime startTime,
            DateTime finishTime,
            TaskResult result)
        {
            this.PlanType = planType;
            this.Definition = definition;
            this.Owner = owner;
            this.StartTime = startTime;
            this.FinishTime = finishTime;
            this.Result = result;
        }

        private ServiceEndpointExecutionData(ServiceEndpointExecutionData executionDataToBeCloned)
        {
            this.Id = executionDataToBeCloned.Id;
            this.PlanType = executionDataToBeCloned.PlanType;
            this.Definition = executionDataToBeCloned.Definition.Clone();
            this.Owner = executionDataToBeCloned.Owner.Clone();
            this.StartTime = executionDataToBeCloned.StartTime;
            this.FinishTime = executionDataToBeCloned.FinishTime;
            this.Result = executionDataToBeCloned.Result;
        }

        /// <summary>
        /// Gets the Id of service endpoint execution data.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int64 Id
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the plan type of service endpoint execution data.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String PlanType
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the definition of service endpoint execution owner.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationOwner Definition
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the owner of service endpoint execution data.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationOwner Owner
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the start time of service endpoint execution.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the finish time of service endpoint execution.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the result of service endpoint execution.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskResult? Result
        {
            get;
            internal set;
        }

        public ServiceEndpointExecutionData Clone()
        {
            return new ServiceEndpointExecutionData(this);
        }
    }
}