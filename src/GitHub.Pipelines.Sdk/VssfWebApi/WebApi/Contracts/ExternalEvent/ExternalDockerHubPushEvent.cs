using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    [ClientInternalUseOnly]
    public class ExternalDockerHubPushEvent
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
        public DockerHubPushData PushData;

        /// <summary>
        /// Name of the repo.
        /// </summary>
        [DataMember]
        public DockerHubRepository Repository;

        /// <summary>
        /// A TFS project ID
        /// </summary>
        [DataMember]
        public String ProjectId;

        /// <summary>
        /// Property bag.  Subscription publisher inputs are copied here.
        /// </summary>
        [DataMember]
        public IDictionary<String, String> Properties;
    }

    [DataContract]
    [ClientInternalUseOnly]
    public class DockerHubPushData
    {
        /// <summary>
        /// Identifer of the repo on the external system.
        /// </summary>
        [DataMember]
        public int PushedAt;

        [DataMember]
        public String Tag;

        [DataMember]
        public String Pusher;
    }

    [DataContract]
    [ClientInternalUseOnly]
    public class DockerHubRepository
    {
        [DataMember]
        public String RepoUrl;

        [DataMember]
        public String Name;

        [DataMember]
        public String Namespace;

        [DataMember]
        public String RepoName;

    }
}
