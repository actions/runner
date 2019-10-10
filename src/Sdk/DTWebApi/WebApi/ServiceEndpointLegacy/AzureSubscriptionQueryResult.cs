using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class AzureSubscriptionQueryResult
    {
        [DataMember]
        [JsonProperty("value")] 
        public List<AzureSubscription> Value;

        [DataMember]
        public string ErrorMessage;
    }
}
