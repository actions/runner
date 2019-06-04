using System;
using System.ComponentModel;

namespace GitHub.Services
{
    internal static class QueryParameters
    {
        //Common query parameters
        internal const String Properties = "properties";

        //Account query parameters
        internal const String CreatorId = "creatorId";
        internal const String OwnerId = "ownerId";
        internal const String IncludeDisabledAccounts = "includeDisabledAccounts";
        internal const String IncludeOwner = "includeOwner";
        internal const String StatusReason = "statusReason";
        internal const String IncludeDeletedUsers = "includeDeletedUsers";
        internal const String AccountId = "accountId";
        internal const String UsePrecreated = "usePrecreated";
        internal const string UserType = "userType";

        //Identity query parameters
        internal const String SubjectDescriptors = "subjectDescriptors";
        internal const String SocialDescriptors = "socialDescriptors";
        internal const String Descriptors = "descriptors";
        internal const String IdentityIds = "identityIds";
        internal const String SearchFilter = "searchFilter";
        internal const String FilterValue = "filterValue";
        internal const String QueryMembership = "queryMembership";
        internal const String IdentitySequenceId = "identitySequenceId";
        internal const String GroupSequenceId = "groupSequenceId";
        internal const String OrgIdentitySequenceId = "organizationIdentitySequenceId";
        internal const String PageSize = "pageSize";
        internal const String ScopeId = "scopeId";
        internal const String ScopeIds = "scopeIds";
        internal const String Recurse = "recurse";
        internal const String Deleted = "deleted";
        internal const String ScopeName = "scopeName";
        internal const String MemberId = "memberId";
        internal const String IncludeRestrictedVisibility = "includeRestrictedVisibility";
        internal const String ReadAllIdentities = "readAllIdentities";
        internal const String ReadIdentitiesOptions = "options";
        internal const String DomainId = "domainId";

        //DelegatedAuthorization query parameters
        internal const String UserId = "userId";
        internal const String DisplayName = "displayName";
        internal const String ValidTo = "validTo";
        internal const String Scope = "scope";
        internal const String AccessTokenKey = "key";
        internal const String TokenType = "tokenType";

        //Security query parameters
        internal const String AlwaysAllowAdministrators = "alwaysAllowAdministrators";
        internal const String Descriptor = "descriptor";
        internal const String IncludeExtendedInfo = "includeExtendedInfo";
        internal const String LocalOnly = "localonly";
        internal const String Token = "token";
        internal const String Tokens = "tokens";
        internal const String Delimiter = "delimiter";

        // Security backing store query parameters
        internal const String OldSequenceId = "oldSequenceId";
        internal const String InheritFlag = "inheritFlag";
        internal const String UseVsidSubjects = "useVsidSubjects";

        //Profile query parameters
        internal const String Size = "size";
        internal const String ModifiedSince = "modifiedsince";
        internal const String ModifiedAfterRevision = "modifiedafterrevision";
        internal const String Partition = "partition";
        internal const String Details = "details";
        internal const String WithCoreAttributes = "withcoreattributes";
        internal const String CoreAttributes = "coreattributes";
        internal const String ProfilePageType = "profilePageType";
        internal const String IpAddress = "ipaddress";

        //ClinetNotification query parameters
        internal const String ClientId = "clientId";

        //File container query parameters
        internal const String ArtifactUris = "artifactUris";
        internal const String ScopeIdentifier = "scope";
        internal const String ItemPath = "itemPath";
        internal const String includeDownloadTickets = "includeDownloadTickets";
        internal const String isShallow = "isShallow";

        //Telemetry query parameters for Licensing
        internal const String TelemetryPrefix = "t-";

    }

    public static class IdentityMruRestApiConstants
    {
        public const String Add = "add";
        public const String Remove = "remove";
        public const String Update = "update";
        public const String Me = "me";
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ProfileRestApiConstants
    {
        public const String Me = "me";
        public const String Value = "value";
    }

    public static class UserRestApiConstants
    {
        public const String Me = "me";
        public const string JsonMergePatchMediaType = "application/merge-patch+json";
    }

    public static class CustomHttpResponseHeaders
    {
        public const string ActivityId = "ActivityId";
    }

    public static class ExtensionManagementConstants
    {
        public const string Me = "me";
    }
}
