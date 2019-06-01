using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using IdentityDescriptor = Microsoft.VisualStudio.Services.Identity.IdentityDescriptor;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    [DataContract]
    public abstract class GraphMember : GraphSubject
    {
        /// <summary>
        /// This represents the name of the container of origin for a graph member. 
        /// (For MSA this is "Windows Live ID", for AD the name of the domain, for AAD the
        /// tenantID of the directory, for VSTS groups the ScopeId, etc)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public string Domain { get; private set; }

        /// <summary>
        /// This is the PrincipalName of this graph member from the source provider. The source 
        /// provider may change this field over time and it is not guaranteed to be immutable
        /// for the life of the graph member by VSTS.
        /// </summary>
        [DataMember]
        public string PrincipalName { get; private set; }

        /// <summary>
        /// The email address of record for a given graph member. This may be different 
        /// than the principal name.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public string MailAddress { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected GraphMember(
            string origin,
            string originId,
            SubjectDescriptor descriptor,
            IdentityDescriptor legacyDescriptor,
            string displayName,
            ReferenceLinks links,
            string url,
            string domain,
            string principalName,
            string mailAddress)
            : base(origin, originId, descriptor, legacyDescriptor, displayName, links, url)
        {
            Domain = domain;
            PrincipalName = principalName;
            MailAddress = mailAddress;
        }

        // only for serialization
        protected GraphMember() { }
    }
}
