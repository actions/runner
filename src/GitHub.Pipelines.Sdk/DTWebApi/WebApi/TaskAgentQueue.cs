using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// An agent queue.
    /// </summary>
    [DataContract]
    public class TaskAgentQueue
    {
        public TaskAgentQueue()
        {
        }

        private TaskAgentQueue(TaskAgentQueue queueToBeCloned)
        {
            this.Id = queueToBeCloned.Id;
            this.ProjectId = queueToBeCloned.ProjectId;
            this.Name = queueToBeCloned.Name;
#pragma warning disable 0618
            this.GroupScopeId = queueToBeCloned.GroupScopeId;
            this.Provisioned = queueToBeCloned.Provisioned;
#pragma warning restore 0618
            if (queueToBeCloned.Pool != null)
            {
                this.Pool = queueToBeCloned.Pool.Clone();
            }
        }

        /// <summary>
        /// ID of the queue
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// Project ID
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid ProjectId
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the queue
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Pool reference for this queue
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }

        #region Obsolete Properties 

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This property is no longer used and will be removed in a future version.", false)]
        public Guid GroupScopeId
        {
            get;
            set;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This property is no longer used and will be removed in a future version.", false)]
        public Boolean Provisioned
        {
            get;
            set;
        }

        #endregion

        public TaskAgentQueue Clone()
        {
            return new TaskAgentQueue(this);
        }
    }
}
