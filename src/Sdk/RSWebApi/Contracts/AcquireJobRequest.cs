using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class AcquireJobRequest
    {
        [DataMember(Name = "jobMessageId", EmitDefaultValue = false)]
        public string JobMessageId { get; set; }

        // This field will be removed in an upcoming Runner release.
        // It's left here temporarily to facilitate the transition to the new field name, JobMessageId.
        [DataMember(Name = "streamId", EmitDefaultValue = false)]
        public string StreamId { get; set; }
    }
}
