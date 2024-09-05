using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class BrokerError
    {
        [DataMember(Name = "source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "errorKind", EmitDefaultValue = false)]
        public string ErrorKind { get; set; }

        [DataMember(Name = "statusCode", EmitDefaultValue = false)]
        public int StatusCode { get; set; }

        [DataMember(Name = "errorMessage", EmitDefaultValue = false)]
        public string Message { get; set; }
    }
}
