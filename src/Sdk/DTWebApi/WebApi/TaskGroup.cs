using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskGroup: TaskDefinition
    {
        /// <summary>
        /// A task group lets you to encapsulate a sequence of tasks already defined in a build definition, a release definition or a task group into a single reusable task.
        /// </summary>
        public TaskGroup()
        {
            this.DefinitionType = TaskDefinitionType.MetaTask;
        }

        private TaskGroup(TaskGroup definition) : base(definition)
        {
            this.DefinitionType = TaskDefinitionType.MetaTask;

            this.Owner = definition.Owner;
            this.Revision = definition.Revision;
            this.CreatedOn = definition.CreatedOn;
            this.ModifiedOn = definition.ModifiedOn;
            this.Comment = definition.Comment;
            this.ParentDefinitionId = definition.ParentDefinitionId;

            if (definition.Tasks != null)
            {
                this.Tasks = new List<TaskGroupStep>(definition.Tasks.Select(x => x.Clone()));
            }

            if (definition.CreatedBy != null)
            {
                this.CreatedBy = definition.CreatedBy.Clone();
            }

            if (definition.ModifiedBy != null)
            {
                this.ModifiedBy = definition.ModifiedBy.Clone();
            }
        }

        public IList<TaskGroupStep> Tasks
        {
            get
            {
                if (m_tasks == null)
                {
                    m_tasks = new List<TaskGroupStep>();
                }

                return m_tasks;
            }
            set
            {
                if (value == null)
                {
                    m_tasks = new List<TaskGroupStep>();
                }
                else
                {
                    this.m_tasks = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets revision.
        /// </summary>
        [DataMember]
        public Int32 Revision
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identity who created.
        /// </summary>
        [DataMember]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets date on which it got created.
        /// </summary>
        [DataMember]
        public DateTime CreatedOn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identity who modified.
        /// </summary>
        [DataMember]
        public IdentityRef ModifiedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets date on which it got modified.
        /// </summary>
        [DataMember]
        public DateTime ModifiedOn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets comment.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Comment
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets parent task group Id. This is used while creating a draft task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid? ParentDefinitionId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets as 'true' to indicate as deleted, 'false' otherwise.  
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Deleted
        {
            get;
            set;
        }

        internal new TaskGroup Clone()
        {
            return new TaskGroup(this);
        }

        /// <summary>
        /// Gets or sets the tasks.
        /// </summary>
        [DataMember(Name = "Tasks")]
        private IList<TaskGroupStep> m_tasks;
    }
}
