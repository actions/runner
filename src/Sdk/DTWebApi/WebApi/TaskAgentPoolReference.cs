using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentPoolReference
    {
        public TaskAgentPoolReference()
        {
        }

        public TaskAgentPoolReference(
            Guid scope,
            Int32 id,
            TaskAgentPoolType poolType = TaskAgentPoolType.Automation)
        {
            this.Id = id;
            this.Scope = scope;
            this.PoolType = poolType;
        }

        protected TaskAgentPoolReference(TaskAgentPoolReference referenceToBeCloned)
        {
            this.Id = referenceToBeCloned.Id;
            this.Name = referenceToBeCloned.Name;
            this.Scope = referenceToBeCloned.Scope;
            this.IsHosted = referenceToBeCloned.IsHosted;
            this.PoolType = referenceToBeCloned.PoolType;
            this.Size = referenceToBeCloned.Size;
            this.IsLegacy = referenceToBeCloned.IsLegacy;
            this.IsInternal = referenceToBeCloned.IsInternal;
        }

        public TaskAgentPoolReference Clone()
        {
            return new TaskAgentPoolReference(this);
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid Scope
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this pool is managed by the service.
        /// </summary>
        [DataMember]
        public Boolean IsHosted
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this pool is internal and can't be modified by users
        /// </summary>
        [DataMember]
        public bool IsInternal
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the pool
        /// </summary>
        [DataMember]
        public TaskAgentPoolType PoolType
        {
            get
            {
                return m_poolType;
            }
            set
            {
                m_poolType = value;
            }
        }

        /// <summary>
        /// Gets the current size of the pool.
        /// </summary>
        [DataMember]
        public Int32 Size
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether the pool is legacy.
        /// </summary>
        [DataMember]
        public Boolean? IsLegacy
        {
            get;
            set;
        }

        private TaskAgentPoolType m_poolType = TaskAgentPoolType.Automation;
    }
}
