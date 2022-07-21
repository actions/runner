
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    public class TaskStep : JobStep
    {
        [JsonConstructor]
        public TaskStep() {
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public override StepType Type => StepType.Task;

        [DataMember]
        public TaskStepDefinitionReference Reference { get; set; }
        [DataMember]
        public IDictionary<string, string> Environment { get; }
        [DataMember]
        public IDictionary<string, string> Inputs { get; }

        [DataMember(EmitDefaultValue = false)]
        public StepTarget Target { get; set; }

        [DataMember]
        public int RetryCountOnTaskFailure { get; set; }

        public override Step Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}