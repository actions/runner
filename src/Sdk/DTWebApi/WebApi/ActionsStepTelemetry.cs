using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Information about a step run on the runner
    /// </summary>
    [DataContract]
    public class ActionsStepTelemetry
    {

        [DataMember(EmitDefaultValue = false)]
        public string Ref { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Type { get; set; }

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
    }
}
