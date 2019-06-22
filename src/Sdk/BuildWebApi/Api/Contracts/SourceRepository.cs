using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a repository returned from a source provider.
    /// </summary>
    [DataContract]
    public class SourceRepository
    {
        /// <summary>
        /// The ID of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id
        {
            get;
            set;
        }

        /// <summary>
        ///  The name of the source provider the repository is from.
        /// </summary>
        [DataMember]
        public String SourceProviderName
        {
            get;
            set;
        }

        /// <summary>
        /// The friendly name of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The full name of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String FullName
        {
            get;
            set;
        }

        /// <summary>
        /// The URL of the repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Url
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the default branch.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DefaultBranch
        {
            get;
            set;
        }

        // TODO: Remove the Properties property. It mainly serves as an area to put provider API URLs that are
        // passed back to the VSTS API so it does not need to construct provider API URLs. This is risky and we
        // should form the URLs ourselves instead of trusting the client.

        /// <summary>
        /// A dictionary that holds additional information about the repository.
        /// </summary>
        public IDictionary<String, String> Properties
        {
            // Warning: This can contain secrets too. As part of #952656, we resolve secrets, it was done considering the fact that this is not a "DataMember"
            // If it's ever made a "DataMember" please be cautious, we would be leaking secrets
            get
            {
                if (m_properties == null)
                {
                    m_properties = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }

                return m_properties;
            }
            internal set
            {
                m_properties = new Dictionary<String, String>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedProperties, ref m_properties, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_properties, ref m_serializedProperties, StringComparer.OrdinalIgnoreCase);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedProperties = null;
        }

        [DataMember(Name = "Properties", EmitDefaultValue = false)]
        private IDictionary<String, String> m_serializedProperties;

        // Warning: This can contain secrets too. As part of #952656, we resolve secrets, it was done considering the fact that this is not a "DataMember"
        // If it's ever made a "DataMember" please be cautious, we would be leaking secrets
        private IDictionary<String, String> m_properties;
    }

    /// <summary>
    /// A set of repositories returned from the source provider.
    /// </summary>
    [DataContract]
    public class SourceRepositories
    {
        /// <summary>
        /// A list of repositories
        /// </summary>
        [DataMember]
        public List<SourceRepository> Repositories
        {
            get;
            set;
        }

        /// <summary>
        /// A token used to continue this paged request; 'null' if the request is complete
        /// </summary>
        [DataMember]
        public String ContinuationToken
        {
            get;
            set;
        }

        /// <summary>
        /// The number of repositories requested for each page
        /// </summary>
        [DataMember]
        public Int32 PageLength
        {
            get;
            set;
        }

        /// <summary>
        /// The total number of pages, or '-1' if unknown
        /// </summary>
        [DataMember]
        public Int32 TotalPageCount
        {
            get;
            set;
        }
    }
}
