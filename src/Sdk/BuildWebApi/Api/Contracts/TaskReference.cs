using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a task.
    /// </summary>
    [DataContract]
    public class TaskReference : BaseSecuredObject
    {
        public TaskReference()
        {
        }

        public TaskReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        protected TaskReference(
            TaskReference taskToBeCloned)
            : base(taskToBeCloned)
        {
            this.Id = taskToBeCloned.Id;
            this.Name = taskToBeCloned.Name;
            this.Version = taskToBeCloned.Version;
        }

        /// <summary>
        /// The ID of the task definition.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the task definition.
        /// </summary>
        [DataMember]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The version of the task definition.
        /// </summary>
        [DataMember]
        public String Version
        {
            get;
            set;
        }

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public virtual TaskReference Clone()
        {
            return new TaskReference(this);
        }
    }
}
