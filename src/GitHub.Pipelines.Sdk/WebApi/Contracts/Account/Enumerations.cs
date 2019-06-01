using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Account
{
    [DataContract]
    public enum AccountType
    {
        [EnumMember]
        Personal = 0,

        [EnumMember]
        Organization = 1
    }

    [DataContract]
    public enum AccountStatus
    {
        [EnumMember]
        None = 0,

        /// <summary>
        /// This hosting account is active and assigned to a customer.
        /// </summary>
        [EnumMember]
        Enabled = 1,

        /// <summary>
        /// This hosting account is disabled.
        /// </summary>
        [EnumMember]
        Disabled = 2,

        /// <summary>
        /// This account is part of deletion batch and scheduled for deletion.
        /// </summary>
        [EnumMember]
        Deleted = 3,

        /// <summary>
        /// This account is not mastered locally and has physically moved.
        /// </summary>
        [EnumMember]
        Moved = 4,
    }

    [DataContract]
    [ClientIncludeModel]
    public enum AccountUserStatus
    {
        [EnumMember]
        None = 0,

        /// <summary>
        /// User has signed in at least once to the VSTS account 
        /// </summary>
        [EnumMember]
        Active = 1,

        /// <summary>
        /// User cannot sign in; primarily used by admin to temporarily remove a user due to absence or license reallocation
        /// </summary>
        [EnumMember]
        Disabled = 2,

        /// <summary>
        /// User is removed from the VSTS account by the VSTS account admin 
        /// </summary>
        [EnumMember]
        Deleted = 3,

        /// <summary>
        /// User is invited to join the VSTS account by the VSTS account admin, but has not signed up/signed in yet 
        /// </summary>
        [EnumMember]
        Pending = 4,

        /// <summary>
        /// User can sign in; primarily used when license is in expired state and we give a grace period
        /// </summary>
        [EnumMember]
        Expired = 5,

        /// <summary>
        /// User is disabled; if reenabled, they will still be in the Pending state
        /// </summary>
        [EnumMember]
        PendingDisabled = 6
    }

    [DataContract]
    public enum AccountLicenseSource
    {
        /// <summary>
        /// The following correspond to various license sources
        /// </summary>
        [EnumMember]
        None = 2,

        [EnumMember]
        VsExpress = 10,

        [EnumMember]
        VsPro = 12,

        [EnumMember]
        VsTestPro = 14,

        [EnumMember]
        VsPremium = 16,

        [EnumMember]
        VsUltimate = 18,

        [EnumMember]
        MSDN = 38,

        [EnumMember]
        MsdnPro = 40,

        [EnumMember]
        MsdnTestPro = 42,

        [EnumMember]
        MsdnPremium = 44,

        [EnumMember]
        MsdnUltimate = 46,

        [EnumMember]
        MsdnPlatforms = 48,

        [EnumMember]
        VSOStandard = 50,

        [EnumMember]
        VSOAdvanced = 52,

        [EnumMember]
        VSOProStandard = 54,

        [EnumMember]
        Win8 = 56,

        [EnumMember]
        Desktop = 58,

        [EnumMember]
        Phone = 60,

        /// <summary>
        /// Early adopters may get a special license
        /// </summary>
        [EnumMember]
        VsEarlyAdopter = 70
    }

    /// <summary>
    /// the defined service rights that can be granted to a user.
    /// </summary>
    [DataContract]
    public enum AccountServiceRights
    {
        [EnumMember]
        StandardLicense = 0,

        [EnumMember]
        AdvancedLicense = 1,

        [EnumMember]
        ProfessionalLicense = 2,

        [EnumMember]
        None = 3,
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PropertyConstants
    {
        public const string ServiceUrlPrefix = "Microsoft.VisualStudio.Services.Account.ServiceUrl.";
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AccountSettingsConstants
    {
        public const string IsAccountServiceLocked = "IsAccountServiceLocked";
        public const string MaximumNumberOfAccountsPerUser = "MaximumNumberOfAccountsPerUser";
    }
}
