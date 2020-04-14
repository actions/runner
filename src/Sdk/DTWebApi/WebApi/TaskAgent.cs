using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// A task agent.
    /// </summary>
    [DataContract]
    public class TaskAgent : TaskAgentReference, ICloneable
    {
        internal TaskAgent()
        {
            this.ProvisioningState = TaskAgentProvisioningStateConstants.Provisioned;
        }

        public TaskAgent(String name)
        {
            this.Name = name;
            this.ProvisioningState = TaskAgentProvisioningStateConstants.Provisioned;
        }

        internal TaskAgent(TaskAgentReference reference)
            : base(reference)
        {
        }

        private TaskAgent(TaskAgent agentToBeCloned)
            : base(agentToBeCloned)
        {
            this.CreatedOn = agentToBeCloned.CreatedOn;
            this.MaxParallelism = agentToBeCloned.MaxParallelism;
            this.StatusChangedOn = agentToBeCloned.StatusChangedOn;

            if (agentToBeCloned.AssignedRequest != null)
            {
                this.AssignedRequest = agentToBeCloned.AssignedRequest.Clone();
            }

            if (agentToBeCloned.Authorization != null)
            {
                this.Authorization = agentToBeCloned.Authorization.Clone();
            }

            if (agentToBeCloned.m_properties != null && agentToBeCloned.m_properties.Count > 0)
            {
                m_properties = new PropertiesCollection(agentToBeCloned.m_properties);
            }

            if (agentToBeCloned.m_labels != null && agentToBeCloned.m_labels.Count > 0)
            {
                m_labels = new HashSet<AgentLabel>(agentToBeCloned.m_labels);
            }
        }

        /// <summary>
        /// Maximum job parallelism allowed for this agent.
        /// </summary>
        [DataMember]
        public Int32? MaxParallelism
        {
            get;
            set;
        }

        /// <summary>
        /// Date on which this agent was created.
        /// </summary>
        [DataMember]
        public DateTime CreatedOn
        {
            get;
            internal set;
        }

        /// <summary>
        /// Date on which the last connectivity status change occurred.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? StatusChangedOn
        {
            get;
            internal set;
        }

        /// <summary>
        /// The request which is currently assigned to this agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentJobRequest AssignedRequest
        {
            get;
            internal set;
        }

        /// <summary>
        /// The last request which was completed by this agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentJobRequest LastCompletedRequest
        {
            get;
            internal set;
        }

        /// <summary>
        /// Authorization information for this agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentAuthorization Authorization
        {
            get;
            set;
        }

        /// <summary>
        /// The labels of the runner
        /// </summary>
        public ISet<AgentLabel> Labels
        {
            get
            {
                if (m_labels == null)
                {
                    m_labels = new HashSet<AgentLabel>();
                }
                return m_labels;
            }
        }

        /// <summary>
        /// Properties which may be used to extend the storage fields available
        /// for a given machine instance.
        /// </summary>
        public PropertiesCollection Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new PropertiesCollection();
                }
                return m_properties;
            }
            internal set
            {
                m_properties = value;
            }
        }

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        public new TaskAgent Clone()
        {
            return new TaskAgent(this);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Labels")]
        private HashSet<AgentLabel> m_labels;
    }
}
