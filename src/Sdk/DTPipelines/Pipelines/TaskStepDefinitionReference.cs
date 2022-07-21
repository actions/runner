
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    public class TaskStepDefinitionReference
    {
        [JsonConstructor]
        public TaskStepDefinitionReference() {

        }

        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Version { get; set; }
        
    }
}