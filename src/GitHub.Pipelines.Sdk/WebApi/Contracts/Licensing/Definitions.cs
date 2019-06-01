using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Licensing
{
    [DataContract]
    public enum AssignmentSource
    {
        None = 0,
        Unknown = 1,
        GroupRule = 2
    }

    [DataContract]
    public enum LicensingOrigin
    {
        None = 0,
        OnDemandPrivateProject = 1,
        OnDemandPublicProject = 2,
        UserHubInvitation = 3,
        PrivateProjectInvitation = 4,
        PublicProjectInvitation = 5,
    }

    [DataContract]
    public enum LicensingSource
    {
        None = 0,
        Account = 1,
        Msdn = 2,
        Profile = 3,
        Auto = 4,
        Trial = 5
    }

    [DataContract]
    [ClientIncludeModel]
    public enum MsdnLicenseType
    {
        None = 0,
        Eligible = 1,
        Professional = 2,
        Platforms = 3,
        TestProfessional = 4,
        Premium = 5,
        Ultimate = 6,
        Enterprise = 7,
    }

    [DataContract]
    [ClientIncludeModel]
    public enum AccountLicenseType
    {
        None = 0,
        EarlyAdopter = 1,
        Express = 2,
        Professional = 3,
        Advanced = 4,
        Stakeholder = 5,
    }

    [DataContract]
    public enum VisualStudioOnlineServiceLevel
    {
        /// <summary>
        /// No service rights. The user cannot access the account
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Default or minimum service level
        /// </summary>
        [EnumMember]
        Express = 1,

        /// <summary>
        /// Premium service level - either by purchasing on the Azure portal or by purchasing the appropriate MSDN subscription
        /// </summary>
        [EnumMember]
        Advanced = 2,

        /// <summary>
        /// Only available to a specific set of MSDN Subscribers
        /// </summary>
        [EnumMember]
        AdvancedPlus = 3,

        /// <summary>
        /// Stakeholder service level
        /// </summary>
        [EnumMember]
        Stakeholder = 4,
    }
}
