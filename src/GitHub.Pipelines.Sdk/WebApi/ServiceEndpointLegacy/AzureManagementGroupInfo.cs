using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public class AzureManagementGroupInfo
    {
        public AzureManagementGroupInfo()
        {
            Properties = new Dictionary<string, string>();
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "type")]
        public String Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public String Name { get; set; }

        public IDictionary<string, string> Properties;
    }
}
