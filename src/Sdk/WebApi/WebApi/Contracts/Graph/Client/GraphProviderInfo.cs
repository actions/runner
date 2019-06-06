using GitHub.Services.Common;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Who is the provider for this user and what is the identifier and 
    /// domain that is used to uniquely identify the user.
    /// </summary>
    [DataContract]
    public class GraphProviderInfo
    {
        /// <summary>
        /// This represents the name of the container of origin for a graph member.
        /// (For MSA this is "Windows Live ID", for AAD the tenantID of the directory.)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public string Domain { get; private set; }

        /// <summary>
        /// The type of source provider for the origin identifier (ex: "aad", "msa")
        /// </summary>
        [DataMember]
        public string Origin { get; private set; }

        /// <summary>
        /// The unique identifier from the system of origin. 
        /// (For MSA this is the PUID in hex notation, for AAD this is the object id.)
        /// </summary>
        [DataMember]
        public string OriginId { get; private set; }

        /// The descriptor is the primary way to reference the graph subject while the system is running.
        public SubjectDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The descriptor is the primary way to reference the graph subject while the system is running. This field
        /// will uniquely identify the same graph subject across both Accounts and Organizations.
        /// </summary>
        [DataMember(Name = "Descriptor", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "Descriptor", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private string DescriptorString
        {
            get { return Descriptor.ToString();  }
            set { Descriptor = SubjectDescriptor.FromString(value); }
        }

        internal GraphProviderInfo(
            string origin, 
            string originId, 
            SubjectDescriptor descriptor, 
            string domain)
        {
            Origin = origin;
            OriginId = originId;
            Descriptor = descriptor;
            Domain = domain;
        }

        // only for serialization
        protected GraphProviderInfo() { }
    }
}
