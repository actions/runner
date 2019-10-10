using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [Obsolete("Use RepositoryProperties instead.")]
    public static class WellKnownRepositoryProperties
    {
        public const String ApiUrl = RepositoryProperties.ApiUrl;
        public const String BranchesUrl = RepositoryProperties.BranchesUrl;
        public const String CheckoutNestedSubmodules = RepositoryProperties.CheckoutNestedSubmodules;
        public const String CleanOptions = RepositoryProperties.CleanOptions;
        public const String CloneUrl = RepositoryProperties.CloneUrl;
        public const String ConnectedServiceId = RepositoryProperties.ConnectedServiceId;
        public const String FetchDepth = RepositoryProperties.FetchDepth;
        public const String Fullname = RepositoryProperties.Fullname;
        public const String GitLfsSupport = RepositoryProperties.GitLfsSupport;
        public const String LabelSources = RepositoryProperties.LabelSources;
        public const String LabelSourcesFormat = RepositoryProperties.LabelSourcesFormat;
        public const String Password = RepositoryProperties.Password;
        public const String SkipSyncSource = RepositoryProperties.SkipSyncSource;
        public const String SvnMapping = RepositoryProperties.SvnMapping;
        public const String TfvcMapping = RepositoryProperties.TfvcMapping;
        public const String TokenType = RepositoryProperties.TokenType;
        public const String Username = RepositoryProperties.Username;
        public const String ReportBuildStatus = RepositoryProperties.ReportBuildStatus;
        public const String AcceptUntrustedCertificates = RepositoryProperties.AcceptUntrustedCertificates;
    }

    [GenerateAllConstants]
    public static class RepositoryProperties
    {
        public const String AcceptUntrustedCertificates = "acceptUntrustedCerts";
        public const String ApiUrl = "apiUrl";
        public const String Archived = "archived";
        public const String BranchesUrl = "branchesUrl";
        public const String CheckoutNestedSubmodules = "checkoutNestedSubmodules";
        public const String CleanOptions = "cleanOptions";
        public const String CloneUrl = "cloneUrl";
        public const String ConnectedServiceId = "connectedServiceId";
        public const String DefaultBranch = "defaultBranch";
        public const String ExternalId = "externalId";
        public const String FetchDepth = "fetchDepth";
        public const String Fullname = "fullName";
        public const String GitLfsSupport = "gitLfsSupport";
        public const String HasAdminPermissions = "hasAdminPermissions";
        public const String IsFork = "isFork";
        public const String IsPrivate = "isPrivate";
        public const String LabelSources = "labelSources";
        public const String LabelSourcesFormat = "labelSourcesFormat";
        public const String Languages = "languages";
        public const String LastUpdated = "lastUpdated";
        public const String ManageUrl = "manageUrl";
        public const String NodeId = "nodeId";
        public const String OwnerAvatarUrl = "ownerAvatarUrl";
        public const String OwnerId = "ownerId";
        public const String OwnerIsAUser = "ownerIsAUser";
        public const String OrgName = "orgName";
        public const String Password = "password";
        public const String PrimaryLanguage = "primaryLanguage";
        public const String RefsUrl = "refsUrl";
        public const String ReportBuildStatus = "reportBuildStatus";
        public const String SafeId = "safeId"; // Used in telemetry, so sensitive information removed (may be a url w/ password)
        public const String SafeRepository = "safeRepository"; // Used in telemetry, so sensitive information removed
        public const String ShortName = "shortName";
        public const String SkipSyncSource = "skipSyncSource";
        public const String SvnMapping = "svnMapping";
        public const String TfvcMapping = "tfvcMapping";
        public const String TokenType = "tokenType";
        public const String Username = "username";
    }
}
