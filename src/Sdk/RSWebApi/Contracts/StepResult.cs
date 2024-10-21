using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using Sdk.RSWebApi.Contracts;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class StepResult
    {
        [DataMember(Name = "external_id", EmitDefaultValue = false)]
        public Guid ExternalID { get; set; }

        [DataMember(Name = "number", EmitDefaultValue = false)]
        public int? Number { get; set; }

        // Example: "Run actions/checkout@v3"
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        // Example: "actions/checkout"
        [DataMember(Name = "action_name", EmitDefaultValue = false)]
        public string ActionName { get; set; }

        [DataMember(Name = "ref", EmitDefaultValue = false)]
        public string Ref { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "status")]
        public TimelineRecordState? Status { get; set; }

        [DataMember(Name = "conclusion")]
        public TaskResult? Conclusion { get; set; }

        [DataMember(Name = "started_at", EmitDefaultValue = false)]
        public DateTime? StartedAt { get; set; }

        [DataMember(Name = "completed_at", EmitDefaultValue = false)]
        public DateTime? CompletedAt { get; set; }

        [DataMember(Name = "completed_log_url", EmitDefaultValue = false)]
        public string CompletedLogURL { get; set; }

        [DataMember(Name = "completed_log_lines", EmitDefaultValue = false)]
        public long? CompletedLogLines { get; set; }

        [DataMember(Name = "annotations", EmitDefaultValue = false)]
        public List<Annotation> Annotations { get; set; }
    }
}
