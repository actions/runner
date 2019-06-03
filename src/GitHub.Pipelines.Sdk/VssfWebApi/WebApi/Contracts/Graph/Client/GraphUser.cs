using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using IdentityDescriptor = GitHub.Services.Identity.IdentityDescriptor;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Graph user entity
    /// </summary>
    [DataContract]
    public class GraphUser : GraphMember
    {
        public override string SubjectKind => Constants.SubjectKind.User;

        /// <summary>
        /// The meta type of the user in the origin, such as "member", "guest", etc.
        /// See <see cref="Constants.UserMetaType"/> for the set of possible values.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string MetaType { get; private set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false), ClientInternalUseOnly]
        internal DateTime MetadataUpdateDate { get; private set; }

        /// <summary>
        /// The short, generally unique name for the user in the backing directory.
        /// For AAD users, this corresponds to the mail nickname, which is often but not necessarily similar
        /// to the part of the user's mail address before the @ sign. 
        /// For GitHub users, this corresponds to the GitHub user handle.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string DirectoryAlias { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeMetadataUpdateDate() => ShoudSerializeInternals;

        /// <summary>
        /// When true, the group has been deleted in the identity provider
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsDeletedInOrigin { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal GraphUser(
            string origin,
            string originId,
            SubjectDescriptor descriptor,
            IdentityDescriptor legacyDescriptor,
            string displayName,
            ReferenceLinks links,
            string url,
            string domain,
            string principalName,
            string mailAddress,
            string metaType,
            DateTime metadataUpdateDate,
            bool isDeletedInOrigin, 
            string directoryAlias)
            : base(origin, originId, descriptor, legacyDescriptor, displayName, links, url, domain, principalName, mailAddress)
        {
            MetaType = metaType;
            MetadataUpdateDate = metadataUpdateDate;
            IsDeletedInOrigin = isDeletedInOrigin;
            DirectoryAlias = directoryAlias;
        }

        // this is how we replace/overwrite parameters and create a new object
        // and keep our internal objects immutable
        internal GraphUser(
            GraphUser user,
            string origin = null,
            string originId = null,
            SubjectDescriptor? descriptor = null,
            IdentityDescriptor legacyDescriptor = null,
            string displayName = null,
            ReferenceLinks links = null,
            string url = null,
            string domain = null,
            string principalName = null,
            string mailAddress = null,
            string metaType = null,
            DateTime? metadataUpdateDate = null,
            bool? isDeletedInOrigin = false,
            string directoryAlias = null)
            : this(origin ?? user?.Origin,
                   originId ?? user?.OriginId,
                   descriptor ?? user?.Descriptor ?? default(SubjectDescriptor),
                   legacyDescriptor ?? user?.LegacyDescriptor ?? default(IdentityDescriptor),
                   displayName ?? user?.DisplayName,
                   links ?? user?.Links,
                   url ?? user?.Url,
                   domain ?? user?.Domain,
                   principalName ?? user?.PrincipalName,
                   mailAddress ?? user?.MailAddress,
                   metaType ?? user?.MetaType,
                   metadataUpdateDate ?? user?.MetadataUpdateDate ?? DateTime.MinValue,
                   isDeletedInOrigin ?? user?.IsDeletedInOrigin ?? default,
                   directoryAlias ?? user?.DirectoryAlias)
        { }

        // only for serialization
        protected GraphUser() { }
    }
}
