using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using IdentityDescriptor = Microsoft.VisualStudio.Services.Identity.IdentityDescriptor;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    /// <summary>
    /// Top-level graph entity
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(GraphSubjectJsonConverter))]
    public abstract class GraphSubject : GraphSubjectBase
    {
        /// <summary>
        /// This field identifies the type of the graph subject (ex: Group, Scope, User).
        /// </summary>
        [DataMember]
        public abstract string SubjectKind { get; }

        /// <summary>
        /// The type of source provider for the origin identifier (ex:AD, AAD, MSA)
        /// </summary>
        [DataMember]
        public string Origin { get; private set; }

        /// <summary>
        /// The unique identifier from the system of origin. Typically a sid, object id or Guid. Linking
        /// and unlinking operations can cause this value to change for a user because the user is not
        /// backed by a different provider and has a different unique id in the new provider. 
        /// </summary>
        [DataMember]
        public string OriginId { get; private set; }
        
        /// <summary>
        /// [Internal Use Only] The legacy descriptor is here in case you need to access old version IMS using identity descriptor.
        /// </summary>
        [ClientInternalUseOnly]
        internal IdentityDescriptor LegacyDescriptor { get; private set; }

        /// <summary>
        /// [Internal Use Only] The legacy descriptor is here in case you need to access old version IMS using identity descriptor.
        /// </summary>
        [DataMember(Name = "LegacyDescriptor", IsRequired = false, EmitDefaultValue = false)]
        private string LegacyDescriptorString
        {
            get { return LegacyDescriptor?.ToString(); }
            set { LegacyDescriptor = IdentityDescriptor.FromString(value); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeLegacyDescriptorString() => ShoudSerializeInternals;
        
        [ClientInternalUseOnly]
        internal bool ShoudSerializeInternals;

        // only for serialization
        protected GraphSubject() { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected GraphSubject(
            string origin,
            string originId,
            SubjectDescriptor descriptor,
            IdentityDescriptor legacyDescriptor,
            string displayName,
            ReferenceLinks links,
            string url) : base(descriptor, displayName, links, url)
        {
            Origin = origin;
            OriginId = originId;
            LegacyDescriptor = legacyDescriptor;
            ShoudSerializeInternals = false;
        }
    }
}
