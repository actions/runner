// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Microsoft.VisualStudio.Services.Identity
{
    [DataContract]
    public enum GroupScopeType
    {
        [EnumMember, XmlEnum("0")]
        Generic = 0,

        [EnumMember, XmlEnum("1")]
        ServiceHost = 1,

        [EnumMember, XmlEnum("2")]
        TeamProject = 2
    }


    // This is the *same* as IdentitySearchFactor in TFCommon
    // Changed the name to avoid conflicts
    [DataContract]
    public enum IdentitySearchFilter
    {
        /// <summary>
        /// NT account name (domain\alias)
        /// </summary>
        [EnumMember]
        AccountName = 0,

        /// <summary>
        /// Display name
        /// </summary>
        [EnumMember]
        DisplayName = 1,

        /// <summary>
        /// Find project admin group
        /// </summary>
        [EnumMember]
        AdministratorsGroup = 2,

        /// <summary>
        /// Find the identity using the identifier
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
        /// Alternate login username
        /// </summary>
        [EnumMember]
        Alias = 6,

        /// <summary>
        /// Find identity using Domain/TenantId
        /// </summary>
        [EnumMember]
        [Obsolete("Use read identities to get member of collection valid users group instead.")]
        Domain = 7,

        /// <summary>
        /// Find identity using DirectoryAlias
        /// </summary>
        [EnumMember]
        DirectoryAlias = 8,

        /// <summary>
        /// Find a team group by its name
        /// </summary>
        [Obsolete("Deprecating TeamGroupName, use LocalGroupName instead and filter out non teams groups from the result")]
        [EnumMember]
        TeamGroupName = 9,

        /// <summary>
        /// Find a local group (i.e. VSTS or TFS rather than AAD or AD group) by its name
        /// </summary>
        [EnumMember]
        LocalGroupName = 10,
    }

    // This enum is as an index for IMS identity caches.
    // This is the *same* as MembershipQuery in TFCommon
    // Changed the name to avoid conflicts
    [DataContract]
    public enum QueryMembership
    {
        // These enumeration values should run from zero to N, with no gaps. 
        // IdentityHostCache uses these values as indexes.

        /// <summary>
        /// Query will not return any membership data
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Query will return only direct membership data
        /// </summary>
        [EnumMember]
        Direct = 1,

        /// <summary>
        /// Query will return expanded membership data
        /// </summary>
        [EnumMember]
        Expanded = 2,

       /// <summary>
        /// Query will return expanded up membership data (parents only)
        /// </summary>
        [EnumMember]
        ExpandedUp = 3,

        /// <summary>
        /// Query will return expanded down membership data (children only)
        /// </summary>
        [EnumMember]
        ExpandedDown = 4

        // Dev10 had the public value "Last = 3", as an indicator of the end of the enumeration.        
        // Dev14 supports public enum value "ExpandedDown = 4"  , as an indicator of the end of the enumeration. 
    }

    // Designates "special" VSS groups.
    [DataContract]
    public enum SpecialGroupType
    {
        [EnumMember]
        Generic = 0,

        [EnumMember]
        AdministrativeApplicationGroup,

        [EnumMember]
        ServiceApplicationGroup,

        [EnumMember]
        EveryoneApplicationGroup,

        [EnumMember]
        LicenseesApplicationGroup,

        [EnumMember]
        AzureActiveDirectoryApplicationGroup,

        [EnumMember]
        AzureActiveDirectoryRole,
    }

    [Flags]
    public enum ReadIdentitiesOptions
    {
        None = 0,
        FilterIllegalMemberships = 1
    }

    public enum RestoreProjectOptions
    {
        /// <summary>
        /// Brings back all memberships whose members are not owned by the scope
        /// </summary>
        All = 0,

        /// <summary>
        /// Brings back some memberships whose members are not owned by the scope.
        /// The membership will be a subset of All with the additional requirement
        /// that the members have visibilty into the project collection scope.
        /// </summary>
        Visible = 1,
    }
}
