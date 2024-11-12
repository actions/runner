using System.Runtime.Serialization;

namespace Sdk.RSWebApi.Contracts
{
    [DataContract]
    public struct Telemetry
    {
        public Telemetry(string message, string type)
        {
            Message = message;
            Type = type;
        }

        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }
    }
}
