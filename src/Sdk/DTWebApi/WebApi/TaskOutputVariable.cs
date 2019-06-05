using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskOutputVariable
    {
        public TaskOutputVariable()
        {
        }

        private TaskOutputVariable(TaskOutputVariable outputDefinitionToClone)
        {
            this.Name = outputDefinitionToClone.Name;
            this.Description = outputDefinitionToClone.Description;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        public TaskOutputVariable Clone()
        {
            return new TaskOutputVariable(this);
        }
    }
}
