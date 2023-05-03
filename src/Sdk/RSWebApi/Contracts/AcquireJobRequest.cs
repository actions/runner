using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class AcquireJobRequest
    {
        [DataMember(Name = "jobMessageId", EmitDefaultValue = false)]
        public string JobMessageId { get; set; }
    }
}
