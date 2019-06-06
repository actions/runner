using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TaskStepDefinitionReference : ITaskDefinitionReference
    {
        [JsonConstructor]
        public TaskStepDefinitionReference()
        {
        }

        private TaskStepDefinitionReference(TaskStepDefinitionReference referenceToClone)
        {
            this.Id = referenceToClone.Id;
            this.Name = referenceToClone.Name;
            this.Version = referenceToClone.Version;
        }

        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember]
        public String Version
        {
            get;
            set;
        }

        public TaskStepDefinitionReference Clone()
        {
            return new TaskStepDefinitionReference(this);
        }
    }
}
