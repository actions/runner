using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.Common
{
    public static class AdminConstants
    {
        /// <summary>
        /// Each incoming web request is assigned a server process id, this constant defines
        /// an element within the Context.Items[] to hold that value.
        /// </summary>
        public const String ServerProcessID = "serverProcessID";
        public const String ApplicationName = "ApplicationName";
    }

    [GenerateSpecificConstants]
    public static class IdentityConstants
    {
        static IdentityConstants()
        {
            // For the normalization of incoming IdentityType strings.
            // This is an optimization; it is not required that any particular IdentityType values
            // appear in this list, but it helps performance to have common values here
            var identityTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { IdentityConstants.WindowsType, IdentityConstants.WindowsType },
                { IdentityConstants.TeamFoundationType, IdentityConstants.TeamFoundationType },
                { IdentityConstants.ClaimsType, IdentityConstants.ClaimsType },
                { IdentityConstants.BindPendingIdentityType, IdentityConstants.BindPendingIdentityType },
                { IdentityConstants.UnauthenticatedIdentityType, IdentityConstants.UnauthenticatedIdentityType },
                { IdentityConstants.ServiceIdentityType, IdentityConstants.ServiceIdentityType },
                { IdentityConstants.AggregateIdentityType, IdentityConstants.AggregateIdentityType },
                { IdentityConstants.ServerTestIdentity, IdentityConstants.ServerTestIdentity },
                { IdentityConstants.ImportedIdentityType, IdentityConstants.ImportedIdentityType },
                { IdentityConstants.GroupScopeType, IdentityConstants.GroupScopeType },
                { IdentityConstants.CspPartnerIdentityType, IdentityConstants.CspPartnerIdentityType },
                { IdentityConstants.System_ServicePrincipal, IdentityConstants.System_ServicePrincipal },
                { IdentityConstants.System_License, IdentityConstants.System_License },
                { IdentityConstants.System_Scope, IdentityConstants.System_Scope },
                { IdentityConstants.PermissionLevelDefinitionType, IdentityConstants.PermissionLevelDefinitionType}
            };

            IdentityTypeMap = identityTypeMap;
        }

        public const string WindowsType = "System.Security.Principal.WindowsIdentity"; // hard coding to make PCL compliant.  typeof(WindowsIdentity).FullName
        public const string TeamFoundationType = "GitHub.Identity";
        public const string ClaimsType = "Microsoft.IdentityModel.Claims.ClaimsIdentity";
        // In WIF 4.5, Microsoft.IdentityModel.Claims.ClaimsIdentity was moved to System.Security.Claims namespace
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const string Wif45ClaimsIdentityType = "System.Security.Claims.ClaimsIdentity";
        public const string AlternateLoginType = "GitHub.Services.Cloud.AlternateLoginIdentity";
        public const string BindPendingIdentityType = "GitHub.BindPendingIdentity";
        public const string ServerTestIdentity = "GitHub.Services.Identity.ServerTestIdentity";
        public const string UnauthenticatedIdentityType = "GitHub.UnauthenticatedIdentity";
        public const string ServiceIdentityType = "GitHub.ServiceIdentity";
        public const string AggregateIdentityType = "GitHub.AggregateIdentity";
        public const string ImportedIdentityType = "GitHub.ImportedIdentity";
        public const string UnknownIdentityType = "GitHub.Services.Identity.UnknownIdentity";
        public const string CspPartnerIdentityType = "GitHub.Claims.CspPartnerIdentity";
        public const string PermissionLevelDefinitionType = "GitHub.Services.PermissionLevel.PermissionLevelIdentity";

        // this is used to represent scopes in the new Graph Rest Api
        public const string GroupScopeType = "GitHub.Services.Graph.GraphScope";

        // These are used with the System Subject Store
        public const string SystemPrefix = "System:";
        public const string System_ServicePrincipal = SystemPrefix + "ServicePrincipal";
        public const string System_WellKnownGroup = SystemPrefix + "WellKnownGroup";
        public const string System_License = SystemPrefix + "License";
        public const string System_Scope = SystemPrefix + "Scope";
        public const string System_CspPartner = SystemPrefix + "CspPartner";
        public const string System_PublicAccess = SystemPrefix + "PublicAccess";

        // This is used to convey an ACE via an IdentityDescriptor
        public const string System_AccessControl = SystemPrefix + "AccessControl";

        public const int MaxIdLength = 256;
        public const int MaxTypeLength = 128;
        public const byte UnknownIdentityTypeId = byte.MaxValue;

        // Social type for identity
        public const byte UnknownSocialTypeId = byte.MaxValue;

        /// <summary>
        ///  Special value for the unique user ID for active (non-deleted) users.
        /// </summary>
        public const int ActiveUniqueId = 0;

        /// <summary>
        ///  Value of attribute that denotes whether user or group.
        /// </summary>
        public const string SchemaClassGroup = "Group";
        public const string SchemaClassUser = "User";

        public const string BindPendingSidPrefix = "upn:";
        [GenerateConstant]
        public const string MsaDomain = "Windows Live ID";
        [GenerateConstant]
        public const string GitHubDomain = "github.com";
        public const string DomainQualifiedAccountNameFormat = "{0}\\{1}";
        public const string MsaSidSuffix = "@Live.com";
        public const string AadOidPrefix = "oid:";
        public const string FrameworkIdentityIdentifierDelimiter = ":";
        public const string IdentityDescriptorPartsSeparator = ";";
        public const string IdentityMinimumResourceVersion = "IdentityMinimumResourceVersion";
        public const int DefaultMinimumResourceVersion = -1;
        public const char DomainAccountNameSeparator = '\\';
        public const bool DefaultUseAccountNameAsDirectoryAlias = true;

        /// <summary>
        /// Values used in switch_hint query parameter to force sign in with personal or work account
        /// </summary>
        public const string SwitchHintQueryKey = "switch_hint";
        public const char SwitchToPersonal = 'P';
        public const char SwitchToWork = 'W';

        public const string AllowNonServiceIdentitiesInDeploymentAdminsGroup =
            nameof(AllowNonServiceIdentitiesInDeploymentAdminsGroup);

        /// <summary>
        /// The DB layer only supports byte, even though the data layer contracts suggests a
        /// 32-bit integer. Note: changing this constant implies that every new identity object
        /// that is created, going forward will have this resource version set. Existing identites
        /// will need to be updated to the current resource version level manually.
        /// 
        /// This is created for rolling out of a feature based on identity not service host. 
        /// This value must be greater than 0. Otherwise, IMS won't update tbl_identityextension for 
        /// identity extended properties. 
        /// </summary>
        public const byte DefaultResourceVersion = 2;

        // Identity ResourceVersions
        [Obsolete]
        public const byte ScopeManifestIssuance = 2;
        [Obsolete]
        public const byte ScopeManifestEnforcementWithInitialGrace = 3;
        [Obsolete]
        public const byte ScopeManifestEnforcementWithoutInitialGrace = 4;

        /// <summary>
        /// The Global scope, [SERVER], represents the highest group Scope ID in the given request context.
        /// For example, [SERVER] at a Deployment context would represent the deployment Scope ID. When
        /// using the global scope in a search, a search for [SERVER]\Team Foundation Administrators
        /// at the deployment level would return the deployment administrators group, while the same call
        /// at the Application host level would return the Account Administrators group. The search will
        /// not recurse down into sub-scopes. 
        /// 
        /// [SERVER] is a deprecated concept, introduced before TFS 2010. We recommend using either the 
        /// collection name in square brackets (i.e. [DefaultCollection] or the scope ID in square brackets
        /// (i.e. [SCOPE_GUID]) instead. 
        /// </summary>
        public const string GlobalScope = "[SERVER]";

        public static readonly Guid LinkedId = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

        public static class EtwIdentityProviderName
        {
            public const string Aad = nameof(Aad);
            public const string Msa = nameof(Msa);
            public const string Vsts = nameof(Vsts);
        }

        public static class EtwIdentityCategory
        {
            public const string AuthenticatedIdentity = nameof(AuthenticatedIdentity);
            public const string UnauthenticatedIdentity = nameof(UnauthenticatedIdentity);
            public const string ServiceIdentity = nameof(ServiceIdentity);
            public const string UnexpectedIdentityType = nameof(UnexpectedIdentityType);
        }

        public static readonly IReadOnlyDictionary<String, String> IdentityTypeMap;
    }
   
    /// <summary>
    /// Common attributes tags used in the collection of properties of TeamFoundationIdentity.
    /// </summary>
    public static class IdentityAttributeTags
    {
        public const string WildCard = "*";

        public const string AccountName = "Account";
        public const string Alias = "Alias";
        public const string CrossProject = "CrossProject";
        public const string Description = "Description";
        public const string Disambiguation = "Disambiguation";
        public const string DistinguishedName = "DN";
        public const string Domain = "Domain";
        public const string GlobalScope = "GlobalScope";
        public const string MailAddress = "Mail";
        public const string RestrictedVisible = "RestrictedVisible";
        public const string SchemaClassName = "SchemaClassName";
        public const string ScopeName = "ScopeName";
        public const string SecurityGroup = "SecurityGroup";
        public const string SpecialType = "SpecialType";
        public const string ScopeId = "ScopeId";
        public const string ScopeType = "ScopeType";
        public const string LocalScopeId = "LocalScopeId";
        public const string SecuringHostId = "SecuringHostId";
        public const string VirtualPlugin = "VirtualPlugin";
        public const string ProviderDisplayName = "ProviderDisplayName";
        public const string IsGroupDeleted = "IsGroupDeleted";

        public const string Cuid = "CUID";
        public const string CuidState = "CUIDState";
        public const string Puid = "PUID";
        public const string Oid = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public const string ConsumerPuid = "ConsumerPUID";
        public const string ComplianceValidated = "ComplianceValidated";
        public const string AuthenticationCredentialValidFrom = "AuthenticationCredentialValidFrom";
        public const string MetadataUpdateDate = "MetadataUpdateDate";
        public const string DirectoryAlias = "DirectoryAlias";
        public const string CacheMaxAge = "CacheMaxAge";
        // temporary used in the ServiceIdentity and CspIdentity
        public const string ServiceStorageKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid";
        public const string ProvData = "prov_data";

        public const string AadRefreshToken = "vss:AadRefreshToken";
        public const string AadRefreshTokenUpdated = "GitHub.Aad.AadRefreshTokenUpdateDate";
        public const string AadUserPrincipalName = "AadUserPrincipalName";
        public const string AcsIdentityProvider = "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider";
        public const string AadIdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";
        public const string IdentityProviderClaim = "http://schemas.microsoft.com/teamfoundationserver/2010/12/claims/identityprovider";
        public const string NameIdentifierClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        public const string TenantIdentifierClaim = "http://schemas.microsoft.com/identity/claims/tenantid";
        public const string AadTenantDisambiguationClaim = "tenant_disambiguate";
        public const string AadMsaPassthroughClaim = "msapt";
        public const string AppidClaim = "appid";

        public const string IdentityTypeClaim = "IdentityTypeClaim";
        public const string IsClientClaim = "IsClient";

        // tbl_IdentityExtension properties.  No longer stored in PropertyService
        public const string ConfirmedNotificationAddress = "ConfirmedNotificationAddress";
        public const string CustomNotificationAddresses = "CustomNotificationAddresses";
        public const string IsDeletedInOrigin = "IsDeletedInOrigin";

        // Extended properties, currently used only for Group images
        public const string ImageId = "GitHub.Identity.Image.Id";
        public const string ImageData = @"GitHub.Identity.Image.Data";
        public const string ImageType = @"GitHub.Identity.Image.Type";
        public const string ImageUploadDate = @"GitHub.Identity.Image.UploadDate";
        public const string CandidateImageData = @"GitHub.Identity.CandidateImage.Data";
        public const string CandidateImageUploadDate = @"GitHub.Identity.CandidateImage.UploadDate";

        // Extended Properties used On Prem
        public const string LastAccessedTime = "LastAccessedTime";

        // Extended Property used by Profile to get the MasterId of an identity.
        // DO NOT USE without consulting with and getting permission from the
        // Identity team.  This is a bad pattern that we are currently supporting
        // for compat with Profile, and the whole concept of MasterIds may be
        // changing with our Sharding work.
        public const string UserId = "UserId";

        // Obsolete extended properties, which should be removed with the next major version (whichever version follows Dev15/TFS 2017)
        [Obsolete] public const string EmailConfirmationSendDates = "EmailConfirmationSendDates";
        [Obsolete] public const string MsdnLicense = "MSDNLicense";
        [Obsolete] public const string BasicAuthPwdKey = "GitHub.Identity.BasicAuthPwd";
        [Obsolete] public const string BasicAuthSaltKey = "GitHub.Identity.BasicAuthSalt";
        [Obsolete] public const string BasicAuthAlgorithm = "Microsoft.TeaFoundation.Identity.BasicAuthAlgorithm";
        [Obsolete] public const string BasicAuthFailures = "Microsoft.TeaFoundation.Identity.BasicAuthFailures";
        [Obsolete] public const string BasicAuthDisabled = "Microsoft.TeaFoundation.Identity.BasicAuthDisabled";
        [Obsolete] public const string BasicAuthPasswordChanges = "GitHub.Identity.BasicAuthSettingsChanges";


        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "When the target .NET framework is revisioned to 4, change return to ISet<String>")]
        public static readonly HashSet<string> ReadOnlyProperties = new HashSet<string>(
            new[]
            {
                AccountName,
                Alias,
                ComplianceValidated,
                CrossProject,
                Description,
                Disambiguation,
                DistinguishedName,
                Domain,
                GlobalScope,
                MailAddress,
                RestrictedVisible,
                SchemaClassName,
                ScopeName,
                SecurityGroup,
                SpecialType,
                ScopeId,
                ScopeType,
                LocalScopeId,
                SecuringHostId,
                Cuid,
                CuidState,
                Puid,
                VirtualPlugin,
                Oid,
                AcsIdentityProvider,
                AadIdentityProvider,
                AadTenantDisambiguationClaim,
                AadMsaPassthroughClaim,
                IdentityProviderClaim,
                NameIdentifierClaim,
                IsClientClaim,
                UserId,
                CacheMaxAge,
                IsGroupDeleted,
            },
            StringComparer.OrdinalIgnoreCase
        );

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "When the target .NET framework is revisioned to 4, change return to ISet<String>")]
        public static readonly HashSet<string> GroupReadOnlyProperties = new HashSet<string>(
            new[]
            {
                Alias,
                ComplianceValidated,
                CrossProject,
                Disambiguation,
                DistinguishedName,
                Domain,
                GlobalScope,
                MailAddress,
                RestrictedVisible,
                SchemaClassName,
                ScopeName,
                SecurityGroup,
                SpecialType,
                ScopeId,
                ScopeType,
                LocalScopeId,
                SecuringHostId,
                Cuid,
                CuidState,
                Puid,
                VirtualPlugin,
                Oid,
                AcsIdentityProvider,
                AadIdentityProvider,
                AadTenantDisambiguationClaim,
                AadMsaPassthroughClaim,
                IdentityProviderClaim,
                NameIdentifierClaim,
                IsClientClaim,
                UserId,
                CacheMaxAge,
                IsGroupDeleted,
            },
            StringComparer.OrdinalIgnoreCase
        );

        [Obsolete]
        public static readonly ISet<string> WhiteListedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static class DirectoryRoleConstants
    {
        /// Name of the directory role that represents "Company Administrator/Global Admin"
        public const string CompanyAdministrator = "Company Administrator";
    }

    // Used with Registration entries
    [GenerateSpecificConstants]
    public static class ToolNames
    {
        public const string Framework = "Framework";
        [GenerateConstant]
        public const string VersionControl = "VersionControl";
        [GenerateConstant]
        public const string WorkItemTracking = "WorkItemTracking";
        [GenerateConstant]
        public const string RemoteWorkItemTracking = "RemoteWorkItemTracking";
        public const string CoreServices = "vstfs";
        public const string Warehouse = "Reports";
        [GenerateConstant]
        public const string TeamBuild = "Build";
        public const string ProxyServer = "ps";
        public const string TeamFoundation = "vstfs";
        public const string SharePoint = "Wss";
        [GenerateConstant]
        public const string TestManagement = "TestManagement";
        public const string LabManagement = "LabManagement";
        public const string ReleaseManagement = "ReleaseManagement";
        public const string SyncService = "SyncService";
        public const string TestRig = "TestRig";
        public const string TSWebAccess = "TSWebAccess";
        public const string ProjectServer = "ProjectServer";
        public const string DeploymentRig = "DeploymentRig";
        public const string TeamProjects = "TeamProjects"; // contains specific project registration entries (project portal, process guidance and doc url)
        public const string Discussion = "Discussion";
        [GenerateConstant]
        public const string Requirements = "Requirements";
        [GenerateConstant]
        public const string Hyperlink = "Hyperlink";
        public const string Classification = "Classification";
        [GenerateConstant]
        public const string Legacy = "Legacy";
        [GenerateConstant]
        public const string CodeSense = "CodeSense";
        [GenerateConstant]
        public const string Git = "Git";
        [GenerateConstant]
        public const string CodeReview = "CodeReview";
        [GenerateConstant]
        public const string ProjectDownload = "ProjectDownload";
        public const string DistributedTask = "DistributedTask";
        [GenerateConstant]
        public const string Wiki = "Wiki";

        public const string Search = "Search";
        [GenerateConstant]
        public const string GitHub = "GitHub";
    }

    // Artifact types
    [GenerateSpecificConstants]
    public static class ArtifactTypeNames
    {
        public const string Project = "TeamProject";
        public const string Node = "Node";
        public const string Collector = "Collector";
        public const string TestResult = "TestResult";
        [GenerateConstant]
        public const string TcmResult = "TcmResult";
        [GenerateConstant]
        public const string TcmResultAttachment = "TcmResultAttachment";
        [GenerateConstant]
        public const string TcmTest = "TcmTest";
        [GenerateConstant]
        public const string Build = "Build";
        public const string BuildAgent = "Agent";
        public const string BuildDefinition = "Definition";
        public const string BuildController = "Controller";
        public const string BuildGroup = "Group";
        public const string BuildRequest = "Request";
        public const string BuildServiceHost = "ServiceHost";
        [GenerateConstant]
        public const string VersionedItem = "VersionedItem";
        [GenerateConstant]
        public const string LatestItemVersion = "LatestItemVersion";
        [GenerateConstant]
        public const string Changeset = "Changeset";
        public const string Label = "Label";
        [GenerateConstant]
        public const string Shelveset = "Shelveset";
        public const string ShelvedItem = "ShelvedItem";
        [GenerateConstant]
        public const string WorkItem = "WorkItem";
        public const string Query = "Query";
        public const string Results = "Results";
        public const string LabEnvironment = "LabEnvironment";
        public const string LabTemplate = "LabTemplate";
        public const string LabSystem = "LabSystem";
        public const string TeamProjectHostGroup = "TeamProjectHostGroup";
        public const string TeamProjectLibraryShare = "TeamProjectLibraryShare";
        public const string TeamProjectCollectionLibraryShare = "TeamProjectCollectionLibraryShare";
        public const string TeamProjectCollectionHostGroup = "TeamProjectCollectionHostGroup";
        public const string TestMachine = "TestMachine";
        [GenerateConstant]
        public const string Storyboard = "Storyboard";
        [GenerateConstant]
        public const string Commit = "Commit";
        public const string LaunchLatestVersionedItem = "LaunchLatestVersionedItem";
        [GenerateConstant]
        public const string CodeReviewId = "CodeReviewId";
        [GenerateConstant]
        public const string CodeReviewSdkId = "ReviewId";
        [GenerateConstant]
        public const string PullRequestId = "PullRequestId";
        [GenerateConstant]
        public const string ProjectDownloadProject = "Project";
        /// <summary>
        /// A Git Ref
        /// </summary>
        [GenerateConstant]
        public const string Ref = "Ref";

        public const string TaskAgentPoolMaintenance = "PoolMaintenance";
        [GenerateConstant]
        public const string WikiPage = "WikiPage";

        // GitHub
        [GenerateConstant]
        public const string PullRequest = "PullRequest";
        [GenerateConstant]
        public const string Issue = "Issue";
    }
    
    /// <summary>
    /// Constant strings used in Notifications
    /// </summary>
    public static class NotificationConstants
    {
        /// <summary>
        /// Macro used in subscriptions which will be replaced by the project name when evaluated
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.MyProjectNameMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String MyProjectNameMacro = "@@MyProjectName@@";

        /// <summary>
        /// Macro used in subscriptions which will be replaced by the subscriber's Display Name when evaluated
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.MyDisplayNameMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String MyDisplayNameMacro = "@@MyDisplayName@@";

        /// <summary>
        /// Macro used in subscriptions which will be replaced by the subscriber's Unique User Name when evaluated
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.MyUniqueNameMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String MyUniqueNameMacro = "@@MyUniqueName@@";

        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.SingleQuoteNameMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String SingleQuoteNameMacro = "@@SQBDQ@@"; //SingleQuoteBetweenDoubleQuotes

        [Obsolete]
        public const String SingleQuoteValue = "\"'\""; //"'"

        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.DoubleQuoteNameMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String DoubleQuoteNameMacro = "@@DQBSQ@@"; //DoubleQuoteBetweenSingleQuotes

        [Obsolete]
        public const String DoubleQuoteValue = "'\"'"; //'"'

        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.SingleQuoteCharMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String SingleQuoteCharMacro = "@@SingleQuote@@";

        [Obsolete]
        public const String SingleQuoteCharValue = "'";

        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.DoubleQuoteCharMacro in assembly MS.VS.Services.Notifications.WebApi")]
        public const String DoubleQuoteCharMacro = "@@DoubleQuote@@";

        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.DoubleQuoteCharValue in assembly MS.VS.Services.Notifications.WebApi")]
        public const String DoubleQuoteCharValue = "\"";

        /// <summary>
        /// Token used in subscription addresses to identify dynamic delivery targets computed from the source event
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.DynamicTargetsToken in assembly MS.VS.Services.Notifications.WebApi")]
        public const String DynamicTargetsToken = "@@";

        /// <summary>
        /// TeamFoundationIdentity property name for a user's custom list of Email addresses to receive notifications at
        /// </summary>
        public const String CustomNotificationAddressesIdentityProperty = "CustomNotificationAddresses";

        /// <summary>
        /// TeamFoundationIdentity propery name for a user's confirmed Email address to receive notifications.  This is used in Hosted environments only.
        /// </summary>
        public const string ConfirmedNotificationAddressIdentityProperty = "ConfirmedNotificationAddress";

        /// <summary>
        /// The name of the WorkItemChangedEvent
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.WorkItemChangedEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const string WorkItemChangedEventTypeName = "WorkItemChangedEvent";

        /// <summary>
        /// The name of the BuildStatusChangedEvent type
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.BuildStatusChangeEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const String BuildStatusChangeEventName = "BuildStatusChangeEvent";

        /// <summary>
        /// The name of the BuildCompletedEvent type
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.BuildCompletedEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const String BuildCompletedEventName = "BuildCompletedEvent";

        /// <summary>
        /// The name of the CheckinEvent type
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.CheckinEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const String CheckinEventName = "CheckinEvent";

        /// <summary>
        /// The name of the CodeReviewChangedEvent type
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.CodeReviewChangedEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const String CodeReviewChangedEventName = "CodeReviewChangedEvent";

        /// <summary>
        /// The name of the GitPushEvent type
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.GitPushEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const String GitPushEventName = "GitPushEvent";

        /// <summary>
        /// The name of the GitPullRequestEvent type
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.LegacyNames.GitPullRequestEvent in assembly MS.VS.Services.Notifications.WebApi")]
        public const String GitPullRequestEventName = "GitPullRequestEvent";

        /// <summary>
        /// The relative path to the alerts admin web page
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationUrlConstants.AlertsPageRelativePath in assembly MS.VS.Services.Notifications.WebApi")]
        public const String AlertsPageRelativePath = "{0}#id={1}&showteams={2}";

        /// <summary>
        /// The alerts page name
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationUrlConstants.AlertsPage in assembly MS.VS.Services.Notifications.WebApi")]
        public const String AlertsPage = "_Alerts";

        /// <summary>
        /// The admin alerts page
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationUrlConstants.AlertsAdminPage in assembly MS.VS.Services.Notifications.WebApi")]
        public const String AlertsAdminPage = "_admin/_Alerts";

        /// <summary>
        /// Property used to keep track of how many confirmations were sent for this user.  Used to limit the number
        /// of confirmations a single user is allowed to send out for their account.
        /// The value is updated and monitored by the SendEmailConfirmationJob.
        /// </summary>
        public const string EmailConfirmationSendDates = "EmailConfirmationSendDates";

        /// <summary>
        /// Prefix to denote that identity field value have been processed
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.ProcessedFlagCharacter in assembly MS.VS.Services.Notifications.WebApi")]
        public const Char ProcessedFlagCharacter = (Char)7;

        /// <summary>
        /// Prefix to denote that identity field value have been processed and converted to TFID
        /// </summary>
        /// [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.ProcessedTfIdFlagCharacter in assembly MS.VS.Services.Notifications.WebApi")]
        public const Char ProcessedTfIdFlagCharacter = (Char)11;

        /// <summary>
        /// Prefix to denote that this is the start of displayname value for this identity field
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.DisplayNameFlagCharacter in assembly MS.VS.Services.Notifications.WebApi")]
        public const Char DisplayNameFlagCharacter = '|';

        /// <summary>
        /// Prefix to denote that this is the start of TFID value for this identity field
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.TfIdFlagCharacter in assembly MS.VS.Services.Notifications.WebApi")]
        public const Char TfIdFlagCharacter = '%';

        /// <summary>
        /// Optional Feature flag to enable escaping Regex expressions when creating Notification subscriptions.
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.FeatureFlags.AllowUserRegexInMatchConditionFeatureFlag in assembly MS.VS.Services.Notifications.WebApi")]
        public const string AllowUserRegexInMatchConditionFeatureFlag = "VisualStudio.Services.Notifications.AllowUserRegexInMatchCondition";

        /// <summary>
        /// The MDM scope name for the notification job
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.MDMNotificationJobScope in assembly MS.VS.Services.Notifications.WebApi")]
        public const string MDMNotificationJobScope = "NotificationJob";

        /// <summary>
        /// Event processing delay KPI name
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.EventProcessingDelayKPI in assembly MS.VS.Services.Notifications.WebApi")]
        public const string EventProcessingDelayKPI = "EventProcessingDelayInMs";

        /// <summary>
        /// Event processing delay KPI description
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.EventProcessingDelayKPIDesc in assembly MS.VS.Services.Notifications.WebApi")]
        public const string EventProcessingDelayKPIDesc = "Time taken to start processing an event";

        /// <summary>
        /// The MDM scope name for the delivery job
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.MDMDeliveryJobscope in assembly MS.VS.Services.Notifications.WebApi")]
        public const string MDMDeliveryJobscope = "NotificationDeliveryJob";

        /// <summary>
        /// Notification delivery delay KPI name
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.DeliveryDelayKPI in assembly MS.VS.Services.Notifications.WebApi")]
        public const string DeliveryDelayKPI = "NotificationDeliveryDelayInMs";

        /// <summary>
        /// Notification delivery delay with retries KPI name
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.DeliveryDelayWithRetriesKPI in assembly MS.VS.Services.Notifications.WebApi")]
        public const string DeliveryDelayWithRetriesKPI = "NotificationDeliveryDelayWithRetriesInMs";

        /// <summary>
        /// Total time taken between the event creation till the notification delivery
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.TotalProcessingTimeKPI in assembly MS.VS.Services.Notifications.WebApi")]
        public const string TotalProcessingTimeKPI = "EventProcessingTimeInMs";

        /// <summary>
        /// Total time taken between the event creation till the notification delivery
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.TotalProcessingTimeWithRetriesKPI in assembly MS.VS.Services.Notifications.WebApi")]
        public const string TotalProcessingTimeWithRetriesKPI = "EventProcessingTimeWithRetriesInMs";

        /// <summary>
        /// Notification delivery delay KPI description
        /// </summary>
        [Obsolete("Moved to GitHub.Services.Notifications.Common.MDMConstants.DeliveryDelayKPIDesc in assembly MS.VS.Services.Notifications.WebApi")]
        public const string DeliveryDelayKPIDesc = "Time taken to start deliverying a notification";

        // caching key for our notification bridge interface
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.BridgeKey in assembly MS.VS.Services.Notifications.WebApi")]
        public const String BridgeKey = "@NotifBridge";

        // delivery retry count registryKey
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.RetryCountRegistryKey in assembly MS.VS.Services.Notifications.WebApi")]
        public const string RetryCountRegistryKey = "NotificationRetryCount";

        // delivery retry count default value
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.RetryCountDefaultValue in assembly MS.VS.Services.Notifications.WebApi")]
        public const Int32 RetryCountDefaultValue = 5;

        // the collection scope Guid
        [Obsolete("Moved to GitHub.Services.Notifications.Common.NotificationFrameworkConstants.CollectionScope in assembly MS.VS.Services.Notifications.WebApi")]
        public static Guid CollectionScope = new Guid("00000000-0000-636f-6c6c-656374696f6e");
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LocationSecurityConstants
    {
        public static readonly Guid NamespaceId = new Guid("2725D2BC-7520-4AF4-B0E3-8D876494731F");
        public static readonly Char PathSeparator = '/';
        public static readonly string NamespaceRootToken = PathSeparator.ToString();
        public static readonly string ServiceDefinitionsToken = String.Concat(NamespaceRootToken, "ServiceDefinitions");
        public static readonly string AccessMappingsToken = String.Concat(NamespaceRootToken, "AccessMappings");

        // Read for ServiceDefinitions and AccessMappings
        public const Int32 Read = 1;
        // Create/Update/Delete for ServiceDefinitions and AccessMappings
        public const Int32 Write = 2;
        public const Int32 AllPermissions = Read | Write;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SecuritySecurityConstants
    {
        public static readonly Guid NamespaceId = new Guid("9A82C708-BFBE-4F31-984C-E860C2196781");
        public const char Separator = '/';
        public const String RootToken = "";

        public const int Read = 1;
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class GraphSecurityConstants
    {
        public static readonly Guid NamespaceId = new Guid("C2EE56C9-E8FA-4CDD-9D48-2C44F697A58E");
        public static readonly string RefsToken = "Refs";
        public static readonly string SubjectsToken = "Subjects";

        public const int ReadByPublicIdentifier = 1;
        public const int ReadByPersonalIdentifier = 2;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TeamProjectSecurityConstants
    {
        public static readonly Guid NamespaceId = new Guid("52D39943-CB85-4d7f-8FA8-C6BAAC873819");

        // Existed in Orcas
        public static readonly Int32 GenericRead = 1;
        public static readonly Int32 GenericWrite = 2;
        public static readonly Int32 Delete = 4;
        public static readonly Int32 PublishTestResults = 8;
        public static readonly Int32 AdministerBuild = 16;
        public static readonly Int32 StartBuild = 32;
        public static readonly Int32 EditBuildStatus = 64;
        public static readonly Int32 UpdateBuild = 128;
        public static readonly Int32 DeleteTestResults = 256;
        public static readonly Int32 ViewTestResults = 512;

        // Dev10 Beta1
        public static readonly Int32 ManageTestEnvironments = 2048;

        // Dev10 Beta2
        public static readonly Int32 ManageTestConfigurations = 4096;

        // Dev14 Update 2 / VSO (M91)
        public static readonly Int32 WorkItemDelete = 8192;

        // Dev14 Update 2 / VSO (M92)
        public static readonly Int32 WorkItemMove = 16384;

        // Dev14 Update 2 / VSO (M94)
        public static readonly Int32 WorkItemPermanentlyDelete = 32768;

        // Dev15 / VSO (M99)
        public static readonly Int32 Rename = 65536;

        /// <summary>
        /// The permission required for setting project properties.
        /// Introduced in Dev15 Update 2 / VSO (M116).
        /// </summary>
        public static readonly Int32 ManageProperties = 131072;

        /// <summary>
        /// The permission required for setting system project properties.
        /// Introduced in Dev15 Update 2 / VSO (M116).
        /// </summary>
        /// <remarks>
        /// This permission was excluded from AllPermissions to avoid being unintentionally granted.
        /// </remarks>
        public static readonly Int32 ManageSystemProperties = 262144;

        /// <summary>
        /// The permission required for bypassing the project property cache.
        /// Introduced in Dev16 / VSO (M118).
        /// </summary>
        /// <remarks>
        /// This permission was excluded from AllPermissions to avoid being unintentionally granted.
        /// </remarks>
        public static readonly Int32 BypassPropertyCache = 524288;

        /// <summary>
        /// The permission required for bypassing the rules  while updating work items.
        /// Introduced in Dev16 / VSO (M126).
        /// </summary>
        public static readonly Int32 BypassRules= 1048576;
        
        /// <summary>
        /// The permission required for suppressing notifications for work item updates.
        /// Introduced in Dev16 / VSO (M126).
        /// </summary>
        public static readonly Int32 SuppressNotifications= 2097152;
        
        /// <summary>
        /// The permission required for updating project visibility. 
        /// Introduced in Dev16 / VSO (M131).
        /// </summary>
        public static readonly Int32 UpdateVisibility = 4194304;

        /// <summary>
        /// The permission required for changing the process of the team project
        /// Introduced in Dev17 / VSO (M136). 
        /// </summary>
        public static readonly Int32 ChangeProjectsProcess = 8388608;

        /// <summary>
        /// The permission required for granting access to backlog management. For stakeholder, this would disabled for private project and enabled for public project.
        /// Introduced in Dev17 / VSO (M137). 
        /// </summary>
        /// <remarks>
        /// This permission was excluded from AllPermissions to avoid being unintentionally granted.
        /// </remarks>
        public static readonly Int32 AgileToolsBacklogManagement = 16777216;

        /// <summary>
        /// The permission required for granting access to backlog management. For stakeholder, this is always disabled.
        /// Introduced in Dev17 / VSO (M150).
        /// </summary>
        /// <remarks>
        /// This permission was excluded from AllPermissions to avoid being unintentionally granted.
        /// </remarks>
        public static readonly Int32 AgileToolsPlans = 33554432;

        public static readonly Int32 AllPermissions =
            GenericRead |
            GenericWrite |
            Delete |
            PublishTestResults |
            AdministerBuild |
            StartBuild |
            EditBuildStatus |
            UpdateBuild |
            DeleteTestResults |
            ViewTestResults |
            ManageTestEnvironments |
            ManageTestConfigurations |
            WorkItemDelete |
            WorkItemMove |
            WorkItemPermanentlyDelete |
            Rename |
            ManageProperties |
            BypassRules |
            SuppressNotifications |
            UpdateVisibility |
            ChangeProjectsProcess;

        public const String ProjectTokenPrefix = "$PROJECT:";

        public static String GetToken(String projectUri)
        {
            if (String.IsNullOrEmpty(projectUri) || !projectUri.StartsWith(ProjectTokenPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (projectUri == null)
                {
                    projectUri = String.Empty;
                }

                return ProjectTokenPrefix + projectUri + ":";
            }

            return projectUri + ":";
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ContentValidationSecurityConstants
    {
        public static readonly Guid NamespaceId = new Guid("B1982126-CB90-4479-BDFD-CBF193241CB8");
        public static readonly string ViolationsToken = "Violations";

        public const int Read = 1;
        public const int Write = 2;
    }

    public enum WinHttpErrorCode
    {
        WINHTTP_ERROR_BASE = 12000,
        WINHTTP_ERROR_LAST = WINHTTP_ERROR_BASE + 188,

        ERROR_WINHTTP_OUT_OF_HANDLES = WINHTTP_ERROR_BASE + 1,
        ERROR_WINHTTP_TIMEOUT = WINHTTP_ERROR_BASE + 2,
        ERROR_WINHTTP_INTERNAL_ERROR = WINHTTP_ERROR_BASE + 4,
        ERROR_WINHTTP_INVALID_URL = WINHTTP_ERROR_BASE + 5,
        ERROR_WINHTTP_UNRECOGNIZED_SCHEME = WINHTTP_ERROR_BASE + 6,
        ERROR_WINHTTP_NAME_NOT_RESOLVED = WINHTTP_ERROR_BASE + 7,
        ERROR_WINHTTP_INVALID_OPTION = WINHTTP_ERROR_BASE + 9,
        ERROR_WINHTTP_OPTION_NOT_SETTABLE = WINHTTP_ERROR_BASE + 11,
        ERROR_WINHTTP_SHUTDOWN = WINHTTP_ERROR_BASE + 12,
        ERROR_WINHTTP_LOGIN_FAILURE = WINHTTP_ERROR_BASE + 15,
        ERROR_WINHTTP_OPERATION_CANCELLED = WINHTTP_ERROR_BASE + 17,
        ERROR_WINHTTP_INCORRECT_HANDLE_TYPE = WINHTTP_ERROR_BASE + 18,
        ERROR_WINHTTP_INCORRECT_HANDLE_STATE = WINHTTP_ERROR_BASE + 19,
        ERROR_WINHTTP_CANNOT_CONNECT = WINHTTP_ERROR_BASE + 29,
        ERROR_WINHTTP_CONNECTION_ERROR = WINHTTP_ERROR_BASE + 30,
        ERROR_WINHTTP_RESEND_REQUEST = WINHTTP_ERROR_BASE + 32,
        ERROR_WINHTTP_SECURE_CERT_DATE_INVALID = WINHTTP_ERROR_BASE + 37,
        ERROR_WINHTTP_SECURE_CERT_CN_INVALID = WINHTTP_ERROR_BASE + 38,
        ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED = WINHTTP_ERROR_BASE + 44,
        ERROR_WINHTTP_SECURE_INVALID_CA = WINHTTP_ERROR_BASE + 45,
        ERROR_WINHTTP_SECURE_CERT_REV_FAILED = WINHTTP_ERROR_BASE + 57,
        ERROR_WINHTTP_CANNOT_CALL_BEFORE_OPEN = WINHTTP_ERROR_BASE + 100,
        ERROR_WINHTTP_CANNOT_CALL_BEFORE_SEND = WINHTTP_ERROR_BASE + 101,
        ERROR_WINHTTP_CANNOT_CALL_AFTER_SEND = WINHTTP_ERROR_BASE + 102,
        ERROR_WINHTTP_CANNOT_CALL_AFTER_OPEN = WINHTTP_ERROR_BASE + 103,
        ERROR_WINHTTP_HEADER_NOT_FOUND = WINHTTP_ERROR_BASE + 150,
        ERROR_WINHTTP_INVALID_SERVER_RESPONSE = WINHTTP_ERROR_BASE + 152,
        ERROR_WINHTTP_INVALID_HEADER = WINHTTP_ERROR_BASE + 153,
        ERROR_WINHTTP_INVALID_QUERY_REQUEST = WINHTTP_ERROR_BASE + 154,
        ERROR_WINHTTP_HEADER_ALREADY_EXISTS = WINHTTP_ERROR_BASE + 155,
        ERROR_WINHTTP_REDIRECT_FAILED = WINHTTP_ERROR_BASE + 156,
        ERROR_WINHTTP_SECURE_CHANNEL_ERROR = WINHTTP_ERROR_BASE + 157,
        ERROR_WINHTTP_BAD_AUTO_PROXY_SCRIPT = WINHTTP_ERROR_BASE + 166,
        ERROR_WINHTTP_UNABLE_TO_DOWNLOAD_SCRIPT = WINHTTP_ERROR_BASE + 167,
        ERROR_WINHTTP_SECURE_INVALID_CERT = WINHTTP_ERROR_BASE + 169,
        ERROR_WINHTTP_SECURE_CERT_REVOKED = WINHTTP_ERROR_BASE + 170,
        ERROR_WINHTTP_NOT_INITIALIZED = WINHTTP_ERROR_BASE + 172,
        ERROR_WINHTTP_SECURE_FAILURE = WINHTTP_ERROR_BASE + 175,
        ERROR_WINHTTP_UNHANDLED_SCRIPT_TYPE = WINHTTP_ERROR_BASE + 176,
        ERROR_WINHTTP_SCRIPT_EXECUTION_ERROR = WINHTTP_ERROR_BASE + 177,
        ERROR_WINHTTP_AUTO_PROXY_SERVICE_ERROR = WINHTTP_ERROR_BASE + 178,
        ERROR_WINHTTP_SECURE_CERT_WRONG_USAGE = WINHTTP_ERROR_BASE + 179,
        ERROR_WINHTTP_AUTODETECTION_FAILED = WINHTTP_ERROR_BASE + 180,
        ERROR_WINHTTP_HEADER_COUNT_EXCEEDED = WINHTTP_ERROR_BASE + 181,
        ERROR_WINHTTP_HEADER_SIZE_OVERFLOW = WINHTTP_ERROR_BASE + 182,
        ERROR_WINHTTP_CHUNKED_ENCODING_HEADER_SIZE_OVERFLOW = WINHTTP_ERROR_BASE + 183,
        ERROR_WINHTTP_RESPONSE_DRAIN_OVERFLOW = WINHTTP_ERROR_BASE + 184,
        ERROR_WINHTTP_CLIENT_CERT_NO_PRIVATE_KEY = WINHTTP_ERROR_BASE + 185,
        ERROR_WINHTTP_CLIENT_CERT_NO_ACCESS_PRIVATE_KEY = WINHTTP_ERROR_BASE + 186,
        ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED_PROXY = WINHTTP_ERROR_BASE + 187,
        ERROR_WINHTTP_SECURE_FAILURE_PROXY = WINHTTP_ERROR_BASE + 188
    }

    public enum CurlErrorCode
    {
        CURLE_OK = 0,
        CURLE_UNSUPPORTED_PROTOCOL = 1,
        CURLE_FAILED_INIT = 2,
        CURLE_URL_MALFORMAT = 3,
        CURLE_NOT_BUILT_IN = 4,
        CURLE_COULDNT_RESOLVE_PROXY = 5,
        CURLE_COULDNT_RESOLVE_HOST = 6,
        CURLE_COULDNT_CONNECT = 7,
        CURLE_FTP_WEIRD_SERVER_REPLY = 8,
        CURLE_REMOTE_ACCESS_DENIED = 9,
        CURLE_FTP_ACCEPT_FAILED = 10,
        CURLE_FTP_WEIRD_PASS_REPLY = 11,
        CURLE_FTP_ACCEPT_TIMEOUT = 12,
        CURLE_FTP_WEIRD_PASV_REPLY = 13,
        CURLE_FTP_WEIRD_227_FORMAT = 14,
        CURLE_FTP_CANT_GET_HOST = 15,
        CURLE_HTTP2 = 16,
        CURLE_FTP_COULDNT_SET_TYPE = 17,
        CURLE_PARTIAL_FILE = 18,
        CURLE_FTP_COULDNT_RETR_FILE = 19,
        CURLE_QUOTE_ERROR = 21,
        CURLE_HTTP_RETURNED_ERROR = 22,
        CURLE_WRITE_ERROR = 23,
        CURLE_UPLOAD_FAILED = 25,
        CURLE_READ_ERROR = 26,
        CURLE_OUT_OF_MEMORY = 27,
        CURLE_OPERATION_TIMEDOUT = 28,
        CURLE_FTP_PORT_FAILED = 30,
        CURLE_FTP_COULDNT_USE_REST = 31,
        CURLE_RANGE_ERROR = 33,
        CURLE_HTTP_POST_ERROR = 34,
        CURLE_SSL_CONNECT_ERROR = 35,
        CURLE_BAD_DOWNLOAD_RESUME = 36,
        CURLE_FILE_COULDNT_READ_FILE = 37,
        CURLE_LDAP_CANNOT_BIND = 38,
        CURLE_LDAP_SEARCH_FAILED = 39,
        CURLE_FUNCTION_NOT_FOUND = 41,
        CURLE_ABORTED_BY_CALLBACK = 42,
        CURLE_BAD_FUNCTION_ARGUMENT = 43,
        CURLE_INTERFACE_FAILED = 45,
        CURLE_TOO_MANY_REDIRECTS = 47,
        CURLE_UNKNOWN_OPTION = 48,
        CURLE_TELNET_OPTION_SYNTAX = 49,
        CURLE_PEER_FAILED_VERIFICATION = 51,
        CURLE_GOT_NOTHING = 52,
        CURLE_SSL_ENGINE_NOTFOUND = 53,
        CURLE_SSL_ENGINE_SETFAILED = 54,
        CURLE_SEND_ERROR = 55,
        CURLE_RECV_ERROR = 56,
        CURLE_SSL_CERTPROBLEM = 58,
        CURLE_SSL_CIPHER = 59,
        CURLE_SSL_CACERT = 60,
        CURLE_BAD_CONTENT_ENCODING = 61,
        CURLE_LDAP_INVALID_URL = 62,
        CURLE_FILESIZE_EXCEEDED = 63,
        CURLE_USE_SSL_FAILED = 64,
        CURLE_SEND_FAIL_REWIND = 65,
        CURLE_SSL_ENGINE_INITFAILED = 66,
        CURLE_LOGIN_DENIED = 67,
        CURLE_TFTP_NOTFOUND = 68,
        CURLE_TFTP_PERM = 69,
        CURLE_REMOTE_DISK_FULL = 70,
        CURLE_TFTP_ILLEGAL = 71,
        CURLE_TFTP_UNKNOWNID = 72,
        CURLE_REMOTE_FILE_EXISTS = 73,
        CURLE_TFTP_NOSUCHUSER = 74,
        CURLE_CONV_FAILED = 75,
        CURLE_CONV_REQD = 76,
        CURLE_SSL_CACERT_BADFILE = 77,
        CURLE_REMOTE_FILE_NOT_FOUND = 78,
        CURLE_SSH = 79,
        CURLE_SSL_SHUTDOWN_FAILED = 80,
        CURLE_AGAIN = 81,
        CURLE_SSL_CRL_BADFILE = 82,
        CURLE_SSL_ISSUER_ERROR = 83,
        CURLE_FTP_PRET_FAILED = 84,
        CURLE_RTSP_CSEQ_ERROR = 85,
        CURLE_RTSP_SESSION_ERROR = 86,
        CURLE_FTP_BAD_FILE_LIST = 87,
        CURLE_CHUNK_FAILED = 88,
        CURLE_NO_CONNECTION_AVAILABLE = 89,
        CURLE_SSL_PINNEDPUBKEYNOTMATCH = 90,
        CURLE_SSL_INVALIDCERTSTATUS = 91,
        CURLE_HTTP2_STREAM = 92,
        CURLE_RECURSIVE_API_CALL = 93
    }
}
