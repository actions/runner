using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Information about a step run on the runner
    /// </summary>
    [DataContract]
    public class ActionsStepTelemetry
    {
        public ActionsStepTelemetry()
        {
            this.ErrorMessages = new List<string>();
        }

        [DataMember(EmitDefaultValue = false)]
        public string Action { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Ref { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Stage { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Guid StepId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string StepContextName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? HasRunsStep { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? HasUsesStep { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEmbedded { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? HasPreStep { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? HasPostStep { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? StepCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TaskResult? Result { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<string> ErrorMessages { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ExecutionTimeInSeconds { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ContainerHookData { get; set; }
    }
}
