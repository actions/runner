using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Graph
{
    /// <summary>
    /// Represents a set of data used to communicate with a federated provider on behalf of a particular user.
    /// </summary>
    [ClientInternalUseOnly]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class GraphFederatedProviderData
    {
        /// <summary>
        /// The descriptor of the graph subject to which this federated provider data corresponds.
        /// </summary>
        public SubjectDescriptor SubjectDescriptor { get; private set; }

        /// <summary>
        /// The descriptor of the graph subject to which this federated provider data corresponds.
        /// </summary>
        [DataMember(Name = nameof(SubjectDescriptor))]
        private string SubjectDescriptorString
        {
            get { return SubjectDescriptor.ToString(); }
            set { SubjectDescriptor = SubjectDescriptor.FromString(value); }
        }
        
        /// <summary>
        /// The name of the federated provider, e.g. "github.com".
        /// </summary>
        [DataMember]
        public string ProviderName { get; private set; }

        /// <summary>
        /// The access token that can be used to communicated with the federated provider
        /// on behalf on the target identity, if we were able to successfully acquire one,
        /// otherwise <code>null</code>, if we were not.
        /// </summary>
        [DataMember]
        public string AccessToken { get; private set; }

        /// <summary>
        /// The version number of this federated provider data, which corresponds to when it was last updated.
        /// Can be used to prevent returning stale provider data from the cache when the caller is aware of a newer version,
        /// such as to prevent local cache poisoning from a remote cache or store.
        /// This is the app layer equivalent of the data layer sequence ID.
        /// </summary>
        [DataMember]
        public long Version { get; private set; }

        public GraphFederatedProviderData(
            SubjectDescriptor subjectDescriptor, 
            string providerName, 
            string accessToken,
            long version)
        {
            SubjectDescriptor = subjectDescriptor;
            ProviderName = providerName;
            AccessToken = accessToken;
            Version = version;
        }
    }
}
