using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskDefinitionReference
    {
        public TaskDefinitionReference()
        {
            // Default is Task
            this.DefinitionType = TaskDefinitionType.Task;
        }

        private TaskDefinitionReference(TaskDefinitionReference definitionReference)
        {
            this.Id = definitionReference.Id;
            this.VersionSpec = definitionReference.VersionSpec;

            // If it is null, we set it to task
            this.DefinitionType = definitionReference.DefinitionType ?? TaskDefinitionType.Task;
        }

        /// <summary>
        /// Gets or sets the unique identifier of task.
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the version specification of task.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String VersionSpec { get; set; }

        /// <summary>
        /// Gets or sets the definition type. Values can be 'task' or 'metaTask'.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String DefinitionType
        {
            get
            {
                return this.m_definitionType ?? (this.m_definitionType = TaskDefinitionType.Task);
            }

            set
            {
                this.m_definitionType = value;
            }
        }

        public override bool Equals(object obj)
        {
            var toEqual = (TaskDefinitionReference)obj;
            if (toEqual == null)
            {
                return false;
            }

            return this.Id.Equals(toEqual.Id) &&
                   (this.VersionSpec?.Equals(toEqual.VersionSpec) ?? this.VersionSpec == toEqual.VersionSpec) &&
                   (this.DefinitionType?.Equals(toEqual.DefinitionType) ?? this.DefinitionType == toEqual.DefinitionType);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        internal TaskDefinitionReference Clone()
        {
            return new TaskDefinitionReference(this);
        }

        private String m_definitionType;
    }
}
