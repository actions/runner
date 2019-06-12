using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TaskTemplateReference
    {
        public TaskTemplateReference()
        {
        }

        private TaskTemplateReference(TaskTemplateReference referenceToClone)
        {
            this.Id = referenceToClone.Id;
            this.Name = referenceToClone.Name;
            this.Version = referenceToClone.Version;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid Id
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

        [DataMember(EmitDefaultValue = false)]
        public String Version
        {
            get;
            set;
        }

        public TaskTemplateReference Clone()
        {
            return new TaskTemplateReference(this);
        }
    }
}
