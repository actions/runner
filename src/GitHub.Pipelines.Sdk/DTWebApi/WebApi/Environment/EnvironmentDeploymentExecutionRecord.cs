using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// EnvironmentDeploymentExecutionRecord.
    /// </summary>
    [DataContract]
    public class EnvironmentDeploymentExecutionRecord
    {
        /// <summary>
        /// Id of the Environment deployment execution history record
        /// </summary>
        [DataMember]
        public Int64 Id
        {
            get;
            set;
        }

        /// <summary>
        /// Request identifier of the Environment deployment execution history record
        /// </summary>
        [DataMember]
        public String RequestIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Id of the Environment
        /// </summary>
        [DataMember]
        public Int32 EnvironmentId
        {
            get;
            set;
        }

        /// <summary>
        /// Service owner Id
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid ServiceOwner
        {
            get;
            set;
        }

        /// <summary>
        /// Project Id
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid ScopeId
        {
            get;
            set;
        }

        /// <summary>
        /// Resource Id
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? ResourceId
        {
            get;
            set;
        }

        /// <summary>
        /// Plan type of the environment deployment execution record
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String PlanType
        {
            get;
            set;
        }

        /// <summary>
        /// Plan Id
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid PlanId
        {
            get;
            set;
        }

        /// <summary>
        /// Stage name
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String StageName
        {
            get;
            set;
        }

        /// <summary>
        /// Job name
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String JobName
        {
            get;
            set;
        }

        /// <summary>
        /// Stage Attempt
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 StageAttempt
        {
            get;
            set;
        }

        /// <summary>
        /// Job Attempt
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobAttempt
        {
            get;
            set;
        }

        /// <summary>
        /// Definition of the environment deployment execution owner
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationOwner Definition
        {
            get;
            set;
        }

        /// <summary>
        /// Owner of the environment deployment execution record
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationOwner Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Result of the environment deployment execution
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskResult? Result
        {
            get;
            set;
        }

        /// <summary>
        /// Queue time of the environment deployment execution
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime QueueTime
        {
            get;
            set;
        }

        /// <summary>
        /// Start time of the environment deployment execution
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            set;
        }

        /// <summary>
        /// Finish time of the environment deployment execution
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            set;
        }
    }
}