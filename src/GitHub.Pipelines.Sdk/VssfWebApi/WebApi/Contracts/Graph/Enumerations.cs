using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Graph
{
    [DataContract]
    public enum GraphTraversalDirection
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        Down = 1,

        [EnumMember]
        Up = 2,
    }

    [DataContract]
    public enum GraphMemberSearchFactor
    {
        /// <summary>
        /// Domain qualified account name (domain\alias)
        /// </summary>
        [EnumMember]
        PrincipalName = 0,

        /// <summary>
        /// Display name
        /// </summary>
        [EnumMember]
        DisplayName = 1,

        /// <summary>
        /// Administrators group
        /// </summary>
        [EnumMember]
        AdministratorsGroup = 2,

        /// <summary>
        /// Find the identity using the identifier (SID)
        /// </summary>
        [EnumMember]
        Identifier = 3,

        /// <summary>
        /// Email address
        /// </summary>
        [EnumMember]
        MailAddress = 4,

        /// <summary>
        /// A general search for an identity. 
        /// </summary>
        /// <remarks>
        /// This is the default search factor for shorter overloads of ReadIdentity, and typically the correct choice for user input.
        /// 
        /// Use the general search factor to find one or more identities by one of the following properties:
        /// * Display name
        /// * account name
        /// * UniqueName
        ///
        /// UniqueName may be easier to type than display name. It can also be used to indicate a single identity when two or more identities share the same display name (e.g. "John Smith")
        /// </remarks>
        [EnumMember]
        General = 5,

        /// <summary>
        /// Alternate login username (Basic Auth Alias)
        /// </summary>
        [EnumMember]
        Alias = 6,

        /// <summary>
        /// Find identity using DirectoryAlias
        /// </summary>
        [EnumMember]
        DirectoryAlias = 8,
    }
}
