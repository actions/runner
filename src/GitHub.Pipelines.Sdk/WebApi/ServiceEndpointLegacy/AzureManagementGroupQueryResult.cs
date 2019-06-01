using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Azure management group query result
    /// </summary>
    [DataContract]
    public class AzureManagementGroupQueryResult
    {
        /// <summary>
        /// List of azure management groups
        /// </summary>
        [DataMember]
        [JsonProperty("value")] 
        public List<AzureManagementGroup> Value;

        /// <summary>
        /// Error message in case of an exception
        /// </summary>
        [DataMember]
        public string ErrorMessage;
    }
}
