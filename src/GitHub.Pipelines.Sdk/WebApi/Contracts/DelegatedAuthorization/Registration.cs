using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    [DataContract]
    public class Registration
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid RegistrationId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid IdentityId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string OrganizationName { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri OrganizationLocation { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string RegistrationName { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string RegistrationDescription { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri RegistrationLocation { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri RegistrationLogoSecureLocation { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri RegistrationTermsOfServiceLocation { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri RegistrationPrivacyStatementLocation { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string ResponseTypes { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Scopes { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid SecretVersionId { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsValid { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IList<Uri> RedirectUris { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ClientType ClientType { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsWellKnown { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal DateTimeOffset? SecretValidTo { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Secret { get; set; }  // This does not return from platform.

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string Issuer { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal DateTimeOffset? ValidFrom { get; set; }

        /// <summary>
        /// Raw cert data string from public key. This will be used for authenticating medium trust clients.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string PublicKey { get; set; }

        public Registration()
        {
            RedirectUris = new List<Uri>(5);
        }
    }
}
