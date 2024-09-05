using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class BrokerError
    {
        [DataMember(Name = "source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "code", EmitDefaultValue = false)]
        public int Code { get; set; }

        [DataMember(Name = "errorMessage", EmitDefaultValue = false)]
        public string Message { get; set; }
    }
}
