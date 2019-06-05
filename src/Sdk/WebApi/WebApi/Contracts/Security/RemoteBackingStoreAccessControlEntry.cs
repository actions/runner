using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Security
{
    /// <summary>
    /// Represents a raw access control entry from a remote backing store.
    /// </summary>
    [DataContract(Name="AccessControlEntryDetails")]
    public sealed class RemoteBackingStoreAccessControlEntry
    {
        public RemoteBackingStoreAccessControlEntry()
        {
        }

        /// <summary>
        /// Creates an AccessControlEntryDetails based on the supplied information.
        /// </summary>
        /// <param name="token">The token of the access control list in which this access control entry belongs</param>
        /// <param name="subject">The identity (or subject) of the access control entry.</param>        
        /// <param name="allow">The allowed permissions for this descriptor.</param>
        /// <param name="deny">The denied permissions for this descriptor.</param>
        /// <param name="isDeleted">True if the ACE has been deleted; false if it is extant.</param>
        public RemoteBackingStoreAccessControlEntry(
            String subject,
            String token,                        
            int allow,
            int deny,
            bool isDeleted)
        {
            this.Subject = subject;
            this.Token = token;
            this.Allow = allow;
            this.Deny = deny;
            this.IsDeleted = isDeleted;
        }

        /// <summary>
        /// The token of the access control list in which this access control entry belongs.
        /// </summary>
        [DataMember]
        public String Token { get; set; }

        /// <summary>
        /// The identity for which the access control entry is allowing / denying permission.
        /// </summary>
        [DataMember(Name="IdentityId")]
        public String Subject { get; set; }

        /// <summary>
        /// The set of permission bits that represent the actions that the associated descriptor is allowed to perform.
        /// </summary>
        [DataMember]
        public int Allow { get; set; }

        /// <summary>
        /// The set of permission bits that represent the actions that the associated descriptor is not allowed to perform.
        /// </summary>
        [DataMember]
        public int Deny { get; set; }

        /// <summary>
        /// True if the ACE has been deleted; used when reading an incremental update from the backing store.
        /// </summary>
        [DataMember]
        public bool IsDeleted { get; set; }
    }
}
