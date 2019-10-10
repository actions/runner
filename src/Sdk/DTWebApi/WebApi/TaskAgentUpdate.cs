using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Details about an agent update.
    /// </summary>
    [DataContract]
    public class TaskAgentUpdate
    {
        internal TaskAgentUpdate()
        {
        }

        private TaskAgentUpdate(TaskAgentUpdate agentUpdateToBeCloned)
        {
            this.CurrentState = agentUpdateToBeCloned.CurrentState;
            if (agentUpdateToBeCloned.SourceVersion != null)
            {
                this.SourceVersion = agentUpdateToBeCloned.SourceVersion.Clone();
            }
            if (agentUpdateToBeCloned.TargetVersion != null)
            {
                this.TargetVersion = agentUpdateToBeCloned.TargetVersion.Clone();
            }
            if (agentUpdateToBeCloned.RequestTime != null)
            {
                this.RequestTime = agentUpdateToBeCloned.RequestTime;
            }
            if (agentUpdateToBeCloned.RequestedBy != null)
            {
                this.RequestedBy = agentUpdateToBeCloned.RequestedBy.Clone();
            }
            if (agentUpdateToBeCloned.Reason != null)
            {
                switch (agentUpdateToBeCloned.Reason.Code)
                {
                    case TaskAgentUpdateReasonType.Manual:
                        this.Reason = (agentUpdateToBeCloned.Reason as TaskAgentManualUpdate).Clone();
                        break;
                    case TaskAgentUpdateReasonType.MinAgentVersionRequired:
                        this.Reason = (agentUpdateToBeCloned.Reason as TaskAgentMinAgentVersionRequiredUpdate).Clone();
                        break;
                }
            }
        }

        /// <summary>
        /// Source agent version of the update.
        /// </summary>
        [DataMember]
        public PackageVersion SourceVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Target agent version of the update.
        /// </summary>
        [DataMember]
        public PackageVersion TargetVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Date on which this update was requested.
        /// </summary>
        [DataMember]
        public DateTime? RequestTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Identity which requested this update.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef RequestedBy
        {
            get;
            internal set;
        }

        /// <summary>
        /// Current state of this agent update.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String CurrentState
        {
            get;
            set;
        }

        /// <summary>
        /// Reason for this update.
        /// </summary>
        [DataMember]
        public TaskAgentUpdateReason Reason
        {
            get;
            set;
        }

        public TaskAgentUpdate Clone()
        {
            return new TaskAgentUpdate(this);
        }
    }
}
