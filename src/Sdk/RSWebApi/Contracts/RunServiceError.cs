using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class RunServiceError
    {
        [DataMember(Name = "source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "statusCode", EmitDefaultValue = false)]
        public int Code { get; set; }

        [DataMember(Name = "errorMessage", EmitDefaultValue = false)]
        public string Message { get; set; }
    }
}
