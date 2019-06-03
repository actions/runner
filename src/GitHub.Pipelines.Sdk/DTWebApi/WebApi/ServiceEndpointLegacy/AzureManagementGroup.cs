using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    ///  Azure Management Group 
    /// </summary>
    [DataContract]
    public class AzureManagementGroup
    {
        /// <summary>
        /// Azure management group name
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "Name")]
        public String Name { get; set; }

        /// <summary>
        /// Id of azure management group
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "Id")]
        public String Id { get; set; }

        /// <summary>
        /// Display name of azure management group
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "displayName")]
        public String DisplayName { get; set; }

        /// <summary>
        /// Id of tenant from which azure management group belogs
        /// </summary>
        [DataMember]
        public String TenantId { get; set; }
    }
}
