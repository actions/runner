using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class AzureSpnOperationStatus
    {
        public AzureSpnOperationStatus(string state, string statusMessage)
        {
            State = state;
            StatusMessage = statusMessage;
        }

        [DataMember] [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [DataMember]
        [JsonProperty(PropertyName = "statusMessage")]
        public string StatusMessage { get; set; }
    }
}
