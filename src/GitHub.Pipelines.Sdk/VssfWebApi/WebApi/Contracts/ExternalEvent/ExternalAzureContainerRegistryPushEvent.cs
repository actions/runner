using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    [ClientInternalUseOnly]
    public class ExternalAzureContainerRegistryPushEvent
    {
        /// <summary>
        /// Current resource version.
        /// </summary>
        [IgnoreDataMember]
        public static ApiResourceVersion CurrentVersion = new ApiResourceVersion(new Version(2, 0), 1);

        /// <summary>
        /// Identifer of the repo on the external system.
        /// </summary>
        [DataMember]
        public AzureContainerRegistryPushData PushData;

        /// <summary>
        /// Identifer of the repo on the external system.
        /// </summary>
        [DataMember]
        public AzureContainerRegistryRepository RepositoryData;

        /// <summary>
        /// Property bag.  Subscription publisher inputs are copied here.
        /// </summary>
        [DataMember]
        public IDictionary<String, String> Properties;
    }

    [DataContract]
    [ClientInternalUseOnly]
    public class AzureContainerRegistryPushData
    {
        [DataMember]
        public string Action { get; set; }
    }

    [DataContract]
    [ClientInternalUseOnly]
    public class AzureContainerRegistryRepository
    {
        [DataMember]
        public String Host { get; set; }

        [DataMember]
        public String Tag { get; set; }

        [DataMember]
        public String RepoName { get; set; }

    }
}
