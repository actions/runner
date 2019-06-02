using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentPoolMaintenanceJob
    {
        internal TaskAgentPoolMaintenanceJob()
        {
        }

        /// <summary>
        /// Id of the maintenance job
        /// </summary>
        [DataMember]
        public Int32 JobId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Pool reference for the maintenance job
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }

        /// <summary>
        /// Orchestration/Plan Id for the maintenance job
        /// </summary>
        [DataMember]
        public Guid OrchestrationId
        {
            get;
            internal set;
        }

        /// <summary>
        /// The maintenance definition for the maintenance job
        /// </summary>
        [DataMember]
        public Int32 DefinitionId
        {
            get;
            set;
        }

        /// <summary>
        /// Status of the maintenance job
        /// </summary>
        [DataMember]
        public TaskAgentPoolMaintenanceJobStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// The maintenance job result
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolMaintenanceJobResult? Result
        {
            get;
            internal set;
        }

        /// <summary>
        /// Time that the maintenance job was queued
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? QueueTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Time that the maintenance job was started
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Time that the maintenance job was completed
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// The identity that queued the maintenance job
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef RequestedBy
        {
            get;
            internal set;
        }

        /// <summary>
        /// The total error counts during the maintenance job
        /// </summary>
        [DataMember]
        public Int32 ErrorCount
        {
            get;
            internal set;
        }

        /// <summary>
        /// The total warning counts during the maintenance job
        /// </summary>
        [DataMember]
        public Int32 WarningCount
        {
            get;
            internal set;
        }

        /// <summary>
        /// The log download url for the maintenance job
        /// </summary>
        [DataMember]
        public String LogsDownloadUrl
        {
            get;
            internal set;
        }


        /// <summary>
        /// All agents that the maintenance job will run on
        /// </summary>
        public List<TaskAgentPoolMaintenanceJobTargetAgent> TargetAgents
        {
            get
            {
                if (m_targetAgents == null)
                {
                    m_targetAgents = new List<TaskAgentPoolMaintenanceJobTargetAgent>();
                }

                return m_targetAgents;
            }
            internal set
            {
                m_targetAgents = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "TargetAgents")]
        private List<TaskAgentPoolMaintenanceJobTargetAgent> m_targetAgents;
    }
}
