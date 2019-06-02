using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Identity
{
    /// <summary>
    /// Identity information.
    /// </summary>
    [DebuggerDisplay("{DisplayName}")]
    [DataContract]
    public class IdentitySelf
    {
        /// <summary>
        /// This is the VSID of the home tenant profile. If the profile is signed into the home tenant or if the profile
        /// has no tenants then this Id is the same as the Id returned by the profile/profiles/me endpoint. Going forward
        /// it is recommended that you use the combined values of Origin, OriginId and Domain to uniquely identify a user 
        /// rather than this Id.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The display name. For AAD accounts with multiple tenants this is the display name of the profile in the home tenant.
        /// </summary>
        [DataMember]
        public String DisplayName
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The UserPrincipalName (UPN) of the account. This value comes from the source provider.
        /// </summary>
        [DataMember]
        public string AccountName
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The type of source provider for the origin identifier.
        /// For MSA accounts this is "msa". For AAD accounts this is "aad".
        /// </summary>
        [DataMember]
        public string Origin
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The unique identifier from the system of origin. If there are multiple tenants this is the 
        /// unique identifier of the account in the home tenant.
        /// (For MSA this is the PUID in hex notation, for AAD this is the object id.)
        /// </summary>
        [DataMember]
        public string OriginId
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// This represents the name of the container of origin.
        /// For AAD accounts this is the tenantID of the home tenant.
        /// For MSA accounts this is the string "Windows Live ID".
        /// </summary>
        [DataMember]
        public string Domain
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// For AAD accounts this is all of the tenants that this account is a member of.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IEnumerable<TenantInfo> Tenants
        {
            get; set;
        }       
    }
}
