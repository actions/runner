using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.Common
{
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
    public class GraphSecurityConstants
    {
        public static readonly Guid NamespaceId = new Guid("C2EE56C9-E8FA-4CDD-9D48-2C44F697A58E");
        public static readonly string RefsToken = "Refs";
        public static readonly string SubjectsToken = "Subjects";

        public const int ReadByPublicIdentifier = 1;
        public const int ReadByPersonalIdentifier = 2;
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
