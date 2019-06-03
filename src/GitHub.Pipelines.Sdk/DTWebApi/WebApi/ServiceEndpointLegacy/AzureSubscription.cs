using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class AzureSubscription
    {
        [DataMember]
        [JsonProperty(PropertyName = "displayName")]
        public String DisplayName { get; set; }

        [DataMember]
        [JsonProperty(PropertyName = "subscriptionId")]
        public String SubscriptionId { get; set; }

        [DataMember]
        public String SubscriptionTenantId { get; set; }

        [DataMember]
        public String SubscriptionTenantName { get; set; }
    }
}
