using GitHub.Services.Identity;
using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Security
{
    /// <summary>
    /// Class for encapsulating the allowed and denied permissions for a given IdentityDescriptor.
    /// </summary>
    [DataContract]
    public sealed class AccessControlEntry
    {
        public AccessControlEntry()
        {
        }

        /// <summary>
        /// Creates an AccessControlEntry based on the supplied information.
        /// </summary>
        /// <param name="descriptor"> The descriptor for the user this AccessControlEntry applies to.</param>
        /// <param name="allow">The allowed permissions for this descriptor.</param>
        /// <param name="deny">The denied permissions for this descriptor.</param>
        /// <param name="extendedInfo">The extended info object to associate with this AccessControlEntry.</param>
        public AccessControlEntry(
            IdentityDescriptor descriptor,
            Int32 allow,
            Int32 deny,
            AceExtendedInformation extendedInfo)
        {
            Descriptor = descriptor;
            Allow = allow;
            Deny = deny;
            ExtendedInfo = extendedInfo;
        }

        /// <summary>
        /// The descriptor for the user this AccessControlEntry applies to.
        /// </summary>
        [DataMember]
        public IdentityDescriptor Descriptor { get; set; }

        /// <summary>
        /// The set of permission bits that represent the actions that the associated descriptor is allowed to perform.
        /// </summary>
        [DataMember]
        public Int32 Allow { get; set; }

        /// <summary>
        /// The set of permission bits that represent the actions that the associated descriptor is not allowed to perform.
        /// </summary>
        [DataMember]
        public Int32 Deny { get; set; }

        /// <summary>
        /// This value, when set, reports the inherited and effective information for 
        /// the associated descriptor. This value is only set on AccessControlEntries returned
        /// by the QueryAccessControlList(s) call when its includeExtendedInfo parameter is set to true.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public AceExtendedInformation ExtendedInfo { get; set; }
    }
}
