// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Reason for disabled resource.
    /// </summary>
    [Flags]
    public enum ResourceStatusReason
    {
        None = 0,
        NoAzureSubscription = 1,
        NoIncludedQuantityLeft = 2,
        SubscriptionDisabled = 4,
        PaidBillingDisabled = 8,
        MaximumQuantityReached = 16
    }

    /// <summary>
    /// Various metered resources in VSTS
    /// </summary>
    public enum ResourceName : int
    {
        StandardLicense = 0,
        AdvancedLicense = 1,
        ProfessionalLicense = 2,
        Build = 3,
        LoadTest = 4,

        //New meters for Build vNext 
        PremiumBuildAgent = 5,
        PrivateOtherBuildAgent = 6,
        PrivateAzureBuildAgent = 7,
        Artifacts = 8,
        MSHostedCICDforMacOS = 9,
        MsHostedCICDforWindowsLinux = 10
    }

    /// <summary>
    /// Azure subscription status
    /// </summary>
    public enum SubscriptionStatus
    {
        Unknown = 0,
        Active = 1,
        Disabled = 2,
        Deleted = 3,
        Unregistered = 4
    }

    public enum SubscriptionAuthorizationSource
    {
        /// <summary>
        /// User has no administrative permissions on the subscription.
        /// </summary>
        Unauthorized = 0,
        /// <summary>
        /// User has administrative permissions on the subscription granted through classic admin/co-admin status.
        /// </summary>
        AdminOrCoAdmin = 1,
        /// <summary>
        /// User has administrative permissions on the subscription granted through role-based access control.
        /// </summary>
        Rbac = 2,

        /// <summary>
        /// Has Special purchase permissions in VSTS.
        /// </summary>
        SpecializedLocalPermission = 3
    }

    /// <summary>
    /// Types of administrative permissions on the subscription granted through role-based access control (RBAC).
    /// </summary
    public enum SubscriptionRolebasedAccessControlSource
    {
        Owner = 0,
        Contributor,
        Other
    }

    /// <summary>
    /// These are known offer types to VSTS.
    /// </summary>
    public enum AzureOfferType
    {
        None = 0,
        Standard = 1,
        Ea = 2,
        Msdn = 3,
        Csp = 4,
        Unsupported = 99
    }

    /// <summary>
    /// The subscription account namespace.
    /// Denotes the 'category' of the account.
    /// </summary>
    public enum AccountProviderNamespace
    {
        VisualStudioOnline = 0,
        AppInsights = 1,
        Marketplace = 2,
        OnPremise = 3
    }

    /// <summary>
    /// Atrribute to group meters under buckets. 
    /// This is attribute in MeteredResource and ISubscriptionResource
    /// </summary>
    public enum MeterGroupType
    {
        License = 0,
        Build = 1,
        LoadTest = 2,
        Artifacts = 3,
        MSHostedCICDforMacOS = 4,
        MsHostedCICDforWindowsLinux = 5
    }

    /// <summary>
    /// The resource billing mode.
    /// </summary>
    public enum ResourceBillingMode
    {
        // TYPO: Not corrected as this is exposed publicly (Committment => Commitment)
        Committment = 0,
        PayAsYouGo = 1
    }

    // TYPO: Not corrected as this is exposed publicly (Frequecy => Frequency)
    /// <summary>
    /// Describes the Renewal frequncy of a Meter.
    /// </summary>
    public enum MeterRenewalFrequecy
    {
        None,
        Monthly,
        Annually
    }

    /// <summary>
    /// Defines meter categories.
    /// </summary>
    public enum MeterCategory
    {
        Legacy,
        Bundle,
        Extension
    }

    /// <summary>
    /// The offer scope.
    /// </summary>
    public enum OfferScope
    {
        Account,
        User,
        UserAccount,
    }

    /// <summary>
    /// The meter billing state.
    /// </summary>
    public enum MeterBillingState
    {
        Free,
        Paid
    }

    /// <summary>
    /// The meter state.
    /// </summary>
    public enum MeterState
    {
        Registered,
        Active,
        Retired,
        Deleted
    }

    /// <summary>
    /// The offer meter assignment model.
    /// </summary>
    public enum OfferMeterAssignmentModel
    {
        /// <summary>
        /// Users need to be explicitly assigned.
        /// </summary>
        Explicit,

        /// <summary>
        /// Users will be added automatically. 
        /// All-or-nothing model.
        /// </summary>
        Implicit
    }

    /// <summary>
    /// The resource renewal group.
    /// </summary>
    public enum ResourceRenewalGroup
    {
        Monthly,
        Jan,
        Feb,
        Mar,
        Apr,
        May,
        Jun,
        Jul,
        Aug,
        Sep,
        Oct,
        Nov,
        Dec
    }

    public enum PurchaseErrorReason
    {
        None = 0,
        MonetaryLimitSet = 1,
        InvalidOfferCode = 2,
        NotAdminOrCoAdmin =3,
        InvalidRegionPurchase = 4,
        PaymentInstrumentNotCreditCard = 5,
        InvalidOfferRegion = 6,
        UnsupportedSubscription = 7,
        DisabledSubscription = 8,
        InvalidUser = 9,
        NotSubscriptionUser = 10,
        UnsupportedSubscriptionCsp = 11,
        TemporarySpendingLimit = 12,
        AzureServiceError = 13
    }

    /// <summary>
    /// The responsible entity/method for billing.
    /// </summary>
    public enum BillingProvider
    {
        SelfManaged = 0,
        AzureStoreManaged = 1
    }

    [DataContract]
    public enum MinimumRequiredServiceLevel
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

    /// <summary>
    /// Commerce event type for reporting.  
    /// </summary>
    public enum CommerceReportingEventType
    {
        Unknown,
        TrialStart,
        TrialEnd,
        TrialExtend,
        NewPurchase,
        UpgradeQuantity,
        DowngradeQuantity,
        CancelPurchase,
        RenewPurchase,
        QuantityChange,
    }

    /// <summary>
    /// Type of purchase request response
    /// </summary>
    public enum PurchaseRequestResponse
    {
        None,
        Approved,
        Denied,
    }
}
