using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
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
            this.CreatedBy = poolToBeCloned.CreatedBy?.Clone();
            this.CreatedOn = poolToBeCloned.CreatedOn;
            this.Id = poolToBeCloned.Id;
            this.IsHosted = poolToBeCloned.IsHosted;
            this.Name = poolToBeCloned.Name;
            this.Scope = poolToBeCloned.Scope;
            this.Size = poolToBeCloned.Size;
            this.PoolType = poolToBeCloned.PoolType;
            this.Owner = poolToBeCloned.Owner?.Clone();
            this.AgentCloudId = poolToBeCloned.AgentCloudId;
            this.TargetSize = poolToBeCloned.TargetSize;
            this.IsLegacy = poolToBeCloned.IsLegacy;

#pragma warning disable 0618
            this.AdministratorsGroup = poolToBeCloned.AdministratorsGroup?.Clone();
            this.GroupScopeId = poolToBeCloned.GroupScopeId;
            this.Provisioned = poolToBeCloned.Provisioned;
            this.ServiceAccountsGroup = poolToBeCloned.ServiceAccountsGroup?.Clone();
#pragma warning restore 0618

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
        /// Creator of the pool. The creator of the pool is automatically added into the 
        /// administrators group for the pool on creation.
        /// </summary>
        [DataMember]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Owner or administrator of the pool.
        /// </summary>
        [DataMember]
        public IdentityRef Owner
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

        #region Obsolete Properties

        /// <summary>
        /// Gets the scope identifier for groups/roles which are owned by this pool.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This property is no longer used and will be removed in a future version.", false)]
        public Guid GroupScopeId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether or not roles have been provisioned for this pool.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This property is no longer used and will be removed in a future version.", false)]
        public Boolean Provisioned
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the administrators group for this agent pool.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This property is no longer used and will be removed in a future version.", false)]
        public IdentityRef AdministratorsGroup
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the service accounts group for this agent pool.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This property is no longer used and will be removed in a future version.", false)]
        public IdentityRef ServiceAccountsGroup
        {
            get;
            internal set;
        }

        #endregion

        public new TaskAgentPool Clone()
        {
            return new TaskAgentPool(this);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;
    }
}
