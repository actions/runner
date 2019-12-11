using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// An organization-level grouping of agents.
    /// </summary>
    [DataContract]
    public class TaskAgentPool : TaskAgentPoolReference
    {
        internal TaskAgentPool()
        {
        }

        public TaskAgentPool(String name)
        {
            this.Name = name;
        }

        private TaskAgentPool(TaskAgentPool poolToBeCloned)
        {
            this.AutoProvision = poolToBeCloned.AutoProvision;
            this.CreatedOn = poolToBeCloned.CreatedOn;
            this.Id = poolToBeCloned.Id;
            this.IsHosted = poolToBeCloned.IsHosted;
            this.Name = poolToBeCloned.Name;
            this.Scope = poolToBeCloned.Scope;
            this.Size = poolToBeCloned.Size;
            this.PoolType = poolToBeCloned.PoolType;
            this.AgentCloudId = poolToBeCloned.AgentCloudId;
            this.TargetSize = poolToBeCloned.TargetSize;
            this.IsLegacy = poolToBeCloned.IsLegacy;

            if (poolToBeCloned.m_properties != null)
            {
                m_properties = new PropertiesCollection(poolToBeCloned.m_properties);
            }
        }

        /// <summary>
        /// The date/time of the pool creation.
        /// </summary>
        [DataMember]
        public DateTime CreatedOn
        {
            get;
            internal set;
        }

        /// <summary>
        /// Whether or not a queue should be automatically provisioned for
        /// each project collection.
        /// </summary>
        [DataMember]
        public Boolean? AutoProvision
        {
            get;
            set;
        }

        /// <summary>
        /// Whether or not the pool should autosize itself based on the
        /// Agent Cloud Provider settings.
        /// </summary>
        [DataMember]
        public Boolean? AutoSize
        {
            get;
            set;
        }

        /// <summary>
        /// Target parallelism.
        /// </summary>
        [DataMember]
        public Int32? TargetSize
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the associated agent cloud.
        /// </summary>
        [DataMember]
        public Int32? AgentCloudId
        {
            get;
            set;
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

        public new TaskAgentPool Clone()
        {
            return new TaskAgentPool(this);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;
    }
}
