using Microsoft.VisualStudio.Services.Identity;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Security
{
    /// <summary>
    /// The AccessControlList class is meant to associate a set of AccessControlEntries with 
    /// a security token and its inheritance settings.
    /// </summary>
    [DataContract]
    public sealed class AccessControlList
    {
        public AccessControlList()
        {
        }

        /// <summary>
        /// Builds an instance of an AccessControlList.
        /// </summary>
        /// <param name="token">The token that this AccessControlList is for.</param>
        /// <param name="inherit">True if this AccessControlList should inherit permissions from its parents.</param>
        /// <param name="acesDictionary">The list of AccessControlEntries that apply to this AccessControlList.</param>
        /// <param name="includeExtendedInfo">True if this ACL will hold AccessControlEntries that have their extended information included.</param>
        public AccessControlList(
            String token,
            Boolean inherit,
            Dictionary<IdentityDescriptor, AccessControlEntry> acesDictionary,
            Boolean includeExtendedInfo)
        {
            Token = token;
            InheritPermissions = inherit;
            AcesDictionary = acesDictionary;
            IncludeExtendedInfo = includeExtendedInfo;
        }

        /// <summary>
        /// True if the given token inherits permissions from parents.
        /// </summary>
        [DataMember]
        public Boolean InheritPermissions { get; set; }

        /// <summary>
        /// The token that this AccessControlList is for.
        /// </summary>
        [DataMember]
        public String Token { get; set; }

        /// <summary>
        /// Storage of permissions keyed on the identity the permission is for.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Dictionary<IdentityDescriptor, AccessControlEntry> AcesDictionary { get; set; }

        /// <summary>
        /// True if this ACL holds ACEs that have extended information.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Boolean IncludeExtendedInfo { get; set; }
    }
}
