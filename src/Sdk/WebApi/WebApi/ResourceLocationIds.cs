using System;
using GitHub.Services.Common;

//each resource (aka "controller") needs its own guid
//for the location service. Defined here so we can use it on the client
//to look it up.
namespace GitHub.Services.Identity
{
    public static class IdentityResourceIds
    {
        public const string AreaId = "8A3D49B8-91F0-46EF-B33D-DDA338C25DB3";
        public const string AreaName = "IMS";
        public static readonly Guid Identity = new Guid("{28010C54-D0C0-4C89-A5B0-1C9E188B9FB7}");
        public const string IdentityResource = "Identities";
        public static readonly Guid IdentityBatch = new Guid("{299E50DF-FE45-4D3A-8B5B-A5836FAC74DC}");
        public const string IdentityBatchResource = "IdentityBatch";
        public static readonly Guid Group = new Guid("{5966283B-4196-4D57-9211-1B68F41EC1C2}");
        public const string GroupResource = "Groups";
        public static readonly Guid Scope = new Guid("{4E11E2BF-1E79-4EB5-8F34-A6337BD0DE38}");
        public const string ScopeResource = "Scopes";
        public const string MemberLocationIdString = "8BA35978-138E-41F8-8963-7B1EA2C5F775";
        public static readonly Guid Member = new Guid(MemberLocationIdString);
        public const string MemberResource = "Members";
        public const string MemberOfLocationIdString = "22865B02-9E4A-479E-9E18-E35B8803B8A0";
        public static readonly Guid MemberOf = new Guid(MemberOfLocationIdString);
        public const string MemberOfResource = "MembersOf";
        public static readonly Guid IdentityDebug = new Guid("{C6B859A5-248C-448A-B770-D373C6E165BD}");
        public const string IdentityDebugResource = "IdentitiesDebug";
        public static readonly Guid IdentitySnapshot = new Guid("{D56223DF-8CCD-45C9-89B4-EDDF692400D7}");
        public const string IdentitySnapshotResource = "IdentitySnapshot";
        public static readonly Guid IdentitySelf = new Guid("{4BB02B5B-C120-4BE2-B68E-21F7C50A4B82}");
        public const string IdentitySelfResource = "me";
        public static readonly Guid SignoutToken = new Guid("{BE39E83C-7529-45E9-9C67-0410885880DA}");
        public const string SignoutTokenResource = "SignoutToken";
        public static readonly Guid SignedInToken = new Guid("{6074FF18-AAAD-4ABB-A41E-5C75F6178057}");
        public const string SignedInTokenResource = "SignedInToken";
        public static readonly Guid IdentityTenant = new Guid("{5F0A1723-2E2C-4C31-8CAE-002D01BDD592}");
        public const string IdentityTenantResource = "tenant";
        public static readonly Guid FrameworkIdentity = new Guid("{DD55F0EB-6EA2-4FE4-9EBE-919E7DD1DFB4}");
        public const string FrameworkIdentityResource = "Identity";
        public static readonly Guid IdentityMaxSequenceId = new Guid("{E4A70778-CB2C-4E85-B7CC-3F3C7AE2D408}");
        public const string IdentityMaxSequenceIdResource = "MaxSequenceId";
        public static readonly Guid Claims = new Guid("{90ddfe71-171c-446c-bf3b-b597cd562afd}");
        public const string ClaimsResource = "Claims";
        public static readonly Guid Rights = new Guid("{05F0AD48-6AEF-42A8-9D9B-9AB650875A5D}");
        public const string RightsResource = "Rights";
        public static readonly Guid RightsBatch = new Guid("{908B4EDC-4C6A-41E8-88ED-07A1F01A9A59}");

        public static readonly Guid DescriptorsResourceLocationId = new Guid("A230389A-94F2-496C-839F-C929787496DD");
        public const string DescriptorsResourceName = "descriptors";

        public static readonly Guid SwapLocationId = new Guid("{7A2338C2-39D8-4906-9889-E8BC9C52CBB2}");
        public const string SwapResource = "Swap";
    }
}

namespace GitHub.Services.Graph
{
    public class GraphResourceIds
    {
        public const string AreaName = "Graph";
        public const string AreaId = "BB1E7EC9-E901-4B68-999A-DE7012B920F8";
        public static readonly Guid AreaIdGuid = new Guid(AreaId);

        public class Groups
        {
            public const string GroupsResourceName = "Groups";
            public static readonly Guid GroupsResourceLocationId = new Guid("EBBE6AF8-0B91-4C13-8CF1-777C14858188");
        }

        public class Descriptors
        {
            public static readonly Guid DescriptorsResourceLocationId = new Guid("048AEE0A-7072-4CDE-AB73-7AF77B1E0B4E");
            public const string DescriptorsResourceName = "Descriptors";
        }

        public class Memberships
        {
            public static readonly Guid MembershipsResourceLocationId = new Guid("3FD2E6CA-FB30-443A-B579-95B19ED0934C");
            public const string MembershipsResourceName = "Memberships";

            public static readonly Guid MembershipsBatchResourceLocationId = new Guid("E34B6394-6B30-4435-94A9-409A5EEF3E31");
            public const string MembershipsBatchResourceName = "MembershipsBatch";

            public static readonly Guid MembershipStatesResourceLocationId = new Guid("1FFE5C94-1144-4191-907B-D0211CAD36A8");
            public const string MembershipStatesResourceName = "MembershipStates";
        }

        public class Scopes
        {
            public const string ScopesResourceName = "Scopes";
            public static readonly Guid ScopesResourceLocationId = new Guid("21B5FEA7-2513-41D0-AF78-B8CDB0F328BB");
        }

        public class SubjectLookup
        {
            public const string SubjectLookupResourceName = "SubjectLookup";
            public static readonly Guid SubjectLookupResourceLocationId = new Guid("4DD4D168-11F2-48C4-83E8-756FA0DE027C");
        }

        public class Users
        {
            public const string UsersResourceName = "Users";
            public static readonly Guid UsersResourceLocationId = new Guid("005E26EC-6B77-4E4F-A986-B3827BF241F5");

            public class ProviderInfo
            {
                public const string ProviderInfoResourceName = "ProviderInfo";
                public static readonly Guid ProviderInfoResourceLocationId = new Guid("1E377995-6FA2-4588-BD64-930186ABDCFA");
            }
        }

        public class Subjects
        {
            public const string SubjectsResourceName = "Subjects";
            public static readonly Guid SubjectsResourceLocationId = new Guid("1D44A2AC-4F8A-459E-83C2-1C92626FB9C6");

            public class Avatars
            {
                public const string AvatarsResourceName = "Avatars";
                public static readonly Guid AvatarsResourceLocationId = new Guid("801EAF9C-0585-4BE8-9CDB-B0EFA074DE91");
            }
        }

        public class Members
        {
            public const string MembersResourceName = "Members";

            public const string MembersByDescriptorResourceLocationIdString = "B9AF63A7-5DB6-4AF8-AAE7-387F775EA9C6";
            public static readonly Guid MembersByDescriptorResourceLocationId = new Guid(MembersByDescriptorResourceLocationIdString);

            public const string MembersResourceLocationIdString = "8B9ECDB2-B752-485A-8418-CC15CF12EE07";
            public static readonly Guid MembersResourceLocationId = new Guid(MembersResourceLocationIdString);
        }

        public class CachePolicies
        {
            public const string CachePoliciesResourceName = "CachePolicies";
            public static readonly Guid CachePoliciesResourceLocationId = new Guid("BEB83272-B415-48E8-AC1E-A9B805760739");
        }

        public class MemberLookup
        {
            public const string MemberLookupResourceName = "MemberLookup";
            public static readonly Guid MemberLookupResourceLocationId = new Guid("3D74D524-AE3D-4D24-A9A7-F8A5CF82347A");
        }

        public class StorageKeys
        {
            public const string StorageKeysResourceName = "StorageKeys";
            public static readonly Guid StorageKeysResourceLocationId = new Guid("EB85F8CC-F0F6-4264-A5B1-FFE2E4D4801F");
        }

        public class MembershipTraversals
        {
            public const string MembershipTraversalsResourceName = "MembershipTraversals";
            public static readonly Guid MembershipTraversalsLocationId = new Guid("5D59D874-746F-4F9B-9459-0E571F1DED8C");
        }

        public class FederatedProviderData
        {
            public static readonly Guid FederatedProviderDataResourceLocationId = new Guid("5DCD28D6-632D-477F-AC6B-398EA9FC2F71");
            public const string FederatedProviderDataResourceName = "FederatedProviderData";
        }
    }
}

namespace GitHub.GraphProfile.WebApi
{
    [GenerateAllConstants]
    public static class GraphProfileResourceIds
    {
        public const String AreaId = "4E40F190-2E3F-4D9F-8331-C7788E833080";
        public const String AreaName = "GraphProfile";
        public static readonly Guid AreaIdGuid = new Guid(AreaId);

        public class MemberAvatars
        {
            public const String MemberAvatarsResourceName = "MemberAvatars";
            public static readonly Guid MemberAvatarsLocationId = new Guid("D443431F-B341-42E4-85CF-A5B0D639ED8F");
        }
    }
}

namespace GitHub.Services.OAuth
{
    public static class TokenOAuth2ResourceIds
    {
        public const string AreaName = "tokenoauth2";
        public const string AreaId = "01c5c153-8bc0-4f07-912a-ec4dc386076d";

        public const string TokenResource = "token";
        public static readonly Guid Token = new Guid("{bbc63806-e448-4e88-8c57-0af77747a323}");
    }

    public static class OAuth2ResourceIds
    {
        public const string AreaName = "oauth2";
        public const string AreaId = "585028FE-17D8-49E2-9A1B-EFB4D8502156";

        public const string TokenResource = "token";
        public static readonly Guid Token = new Guid("{CD12634C-1D0F-4A19-9FF3-17875B764932}");
    }
}

namespace GitHub.Services.Tokens
{
    public static class TokenAuthResourceIds
    {
        public const string AreaName = "TokenAuth";
        public const string AreaId = "c5a2d98b-985c-432e-825e-3c6971edae87";

        public const string AuthorizationResource = "Authorizations";
        public static readonly Guid Authorization = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");

        public const string HostAuthorizationResource = "HostAuthorization";
        public static readonly Guid HostAuthorizeId = new Guid("{817d2b46-1507-4efe-be2b-adccf17ffd3b}");

        public const string RegistrationResource = "Registration";
        public static readonly Guid Registration = new Guid("{522ad1a0-389d-4c6f-90da-b145fd2d3ad8}");

        public const string RegistrationSecretResource = "RegistrationSecret";
        public static readonly Guid RegistrationSecret = new Guid("{74896548-9cdd-4315-8aeb-9ecd88fceb21}");
    }

    public static class TokenIssueResourceIds
    {
        public const string AreaName = "TokenIssue";
        public const string AreaId = "6b10046c-829d-44d2-8a1d-02f88f4ff032";

        public const string AccessTokenResource = "AccessTokens";
        public static readonly Guid AccessToken = new Guid("{24691e90-c8bd-42c0-8aae-71b7511a797a}");

        public const string SessionTokenResource = "SessionTokens";
        public static readonly Guid SessionToken = new Guid("{98e25729-952a-4b1f-ac89-7ca8b9803261}");

        public const string AadUserTokenResource = "AadUserTokens";
        public static readonly Guid AadUserToken = new Guid("{4cbff9ec-7f69-4d40-a82e-cca1e8545d01}");

        public const string AadAppTokenResource = "AadAppTokens";
        public static readonly Guid AadAppToken = new Guid("{f15de83d-4b1d-4387-90aa-e72c0ce10b3e}");

        public const string AppSessionTokenResource = "AppSessionTokens";
        public static readonly Guid AppSessionToken = new Guid("{325f73ea-e978-4ad1-8f3a-c30b39000a17}");

        public const string AppTokenPairResource = "AppTokenPairs";
        public static readonly Guid AppTokenPair = new Guid("{9030cb81-c1fd-4f3b-9910-c90eb559b830}");
    }

    public static class TokenTokenExpirationResourceIds
    {
        public const string AreaName = "TokenExpiration";
        public const string AreaId = "339c63b0-d305-4fd3-958a-22b8e0eb6fc2";

        public const string TokenExpirationResource = "Token";
        public static readonly Guid TokenExpiration = new Guid("{e04f61f2-a03d-4aec-8b0f-6e8511fe5adc}");
    }

    public static class DelegatedAuthResourceIds
    {
        public const string AreaName = "DelegatedAuth";
        public const string AreaId = "A0848FA1-3593-4AEC-949C-694C73F4C4CE";

        public const string AuthorizationResource = "Authorizations";
        public static readonly Guid Authorization = new Guid("EFBF6E0C-1150-43FD-B869-7E2B04FC0D09");

        public const string HostAuthorizationResource = "HostAuthorization";
        public static readonly Guid HostAuthorizeId = new Guid("{7372FDD9-238C-467C-B0F2-995F4BFE0D94}");

        public const string RegistrationResource = "Registration";
        public static readonly Guid Registration = new Guid("{909CD090-3005-480D-A1B4-220B76CB0AFE}");

        public const string RegistrationSecretResource = "RegistrationSecret";
        public static readonly Guid RegistrationSecret = new Guid("{F37E5023-DFBE-490E-9E40-7B7FB6B67887}");
    }

    public static class TokenResourceIds
    {
        public const string AreaName = "Token";
        public const string AreaId = "0AD75E84-88AE-4325-84B5-EBB30910283C";

        public const string AccessTokenResource = "AccessTokens";
        public static readonly Guid AccessToken = new Guid("{94C2BCFB-BF10-4B41-AC01-738122D6B5E0}");

        public const string SessionTokenResource = "SessionTokens";
        public static readonly Guid SessionToken = new Guid("{ADA996BC-8C18-4193-B20C-CD41B13F5B4D}");

        public const string AadUserTokenResource = "AadUserTokens";
        public static readonly Guid AadUserToken = new Guid("{6A8B6E50-FDA9-4AC1-9536-678C28BE2F7D}");

        public const string AadAppTokenResource = "AadAppTokens";
        public static readonly Guid AadAppToken = new Guid("{11B3E525-35D3-4373-8985-EA72887427DB}");

        public const string AppSessionTokenResource = "AppSessionTokens";
        public static readonly Guid AppSessionToken = new Guid("{B743B207-6DC5-457B-B1DF-B9B63D640F0B}");

        public const string AppTokenPairResource = "AppTokenPairs";
        public static readonly Guid AppTokenPair = new Guid("{9CE3C96A-34A2-41AF-807D-205DA73F227B}");
    }

    public static class PropertyCacheResourceIds
    {
        public const string AreaName = "Cache";
        public const string AreaId = "0B808CEB-EF49-4C5E-9483-600A4ECF1224";

        public const string PropertyCacheResource = "Properties";
        public static readonly Guid PropertyCache = new Guid("{656342EB-AE7D-4FF2-802F-19C6E35B0FE6}");
    }
}

namespace GitHub.Services.TokenAdmin.Client
{
    public static class TokenAdminResourceIds
    {
        public const string AreaName = "TokenAdmin";
        public const string AreaId = "af68438b-ed04-4407-9eb6-f1dbae3f922e";

        public const string PersonalAccessTokensResource = "PersonalAccessTokens";
        public static readonly Guid PersonalAccessTokensLocationId = new Guid("{af68438b-ed04-4407-9eb6-f1dbae3f922e}");

        public const string RevocationsResource = "Revocations";
        public static readonly Guid RevocationsLocationId = new Guid("{a9c08b2c-5466-4e22-8626-1ff304ffdf0f}");

        public const string RevocationRulesResource = "RevocationRules";
        public static readonly Guid RevocationRulesLocationId = new Guid("{ee4afb16-e7ab-4ed8-9d4b-4ef3e78f97e4}");
    }
}

namespace GitHub.Services.Tokens.TokenAdmin.Client
{
    public static class TokenAdministrationResourceIds
    {
        public const string TokenAreaName = "TokenAdministration";
        public const string TokenAreaId = "95935461-9E54-44BD-B9FB-04F4DD05D640";

        public const string TokenPersonalAccessTokensResource = "TokenPersonalAccessTokens";
        public static readonly Guid TokenPersonalAccessTokensLocationId = new Guid("{1BB7DB14-87C5-4762-BF77-A70AD34A9AB3}");

        public const string TokenRevocationsResource = "TokenRevocations";
        public static readonly Guid TokenRevocationsLocationId = new Guid("{A2E4520B-1CC8-4526-871E-F3A8F865F221}");

        public const string TokenListGlobalIdentities = "TokenListGlobalIdentities";
        public static readonly Guid TokenListGlobalIdentitiesId = new Guid("{30D3A12B-66C3-4669-B016-ECB0706C8D0F}");
    }
}

namespace GitHub.Services.Identity.Client
{
    public static class PropertyCacheResourceIds
    {
        public const string AreaName = "Cache";
        public const string AreaId = "0B808CEB-EF49-4C5E-9483-600A4ECF1224";

        public const string PropertyCacheResource = "Properties";
        public static readonly Guid PropertyCache = new Guid("{656342EB-AE7D-4FF2-802F-19C6E35B0FE6}");
    }
}

namespace GitHub.Services.Security
{
    public static class LocationResourceIds
    {
        public const string SecurityBackingStoreArea = "SBS";

        public const string SecurityBackingStoreNamespaceResource = "SBSNamespace";
        public static readonly Guid SecurityBackingStoreNamespace = new Guid("049929B0-79E1-4AD5-A548-9E192D5C049E");

        public const string SecurityBackingStoreAclStoreResource = "SBSAclStore";
        public static readonly Guid SecurityBackingStoreAclStore = new Guid("D9DA18E4-274B-4DD4-B09D-B8B931AF3826");

        public const string SecurityBackingStoreAclsResource = "SBSAcls";
        public static readonly Guid SecurityBackingStoreAcls = new Guid("3F95720D-2EF6-47CC-B5D7-733561D13EB9");

        public const string SecurityBackingStoreAcesResource = "SBSAces";
        public static readonly Guid SecurityBackingStoreAces = new Guid("AB821A2B-F383-4C72-8274-8425ED30835D");

        public const string SecurityBackingStoreInheritResource = "SBSInherit";
        public static readonly Guid SecurityBackingStoreInherit = new Guid("25DCFFD2-9F2A-4109-B4CC-000F8472107D");

        public const string SecurityBackingStoreTokensResource = "SBSTokens";
        public static readonly Guid SecurityBackingStoreTokens = new Guid("466ECEAD-D7F1-447C-8BC1-52C22592B98E");

        public const string SecurityServiceArea = "Security";

        public const string SecurityPermissionsResource = "Permissions";
        public static readonly Guid SecurityPermissions = new Guid("DD3B8BD6-C7FC-4CBD-929A-933D9C011C9D");

        public const string SecurityPermissionEvaluationBatchResource = "PermissionEvaluationBatch";
        public static readonly Guid SecurityPermissionEvaluationBatch = new Guid("CF1FAA59-1B63-4448-BF04-13D981A46F5D");

        public const string SecurityAccessControlEntriesResource = "AccessControlEntries";
        public static readonly Guid SecurityAccessControlEntries = new Guid("AC08C8FF-4323-4B08-AF90-BCD018D380CE");

        public const string SecurityAccessControlListsResource = "AccessControlLists";
        public static readonly Guid SecurityAccessControlLists = new Guid("18A2AD18-7571-46AE-BEC7-0C7DA1495885");

        public const string SecurityNamespacesResource = "SecurityNamespaces";
        public static readonly Guid SecurityNamespaces = new Guid("CE7B9F95-FDE9-4BE8-A86D-83B366F0B87A");
    }
}

namespace GitHub.Services.Account
{
    public static class AccountResourceIds
    {
        public const string RegionArea = "Region";

        public const string AreaId = "0D55247A-1C47-4462-9B1F-5E2125590EE6";

        public const string AccountServiceArea = "Account";
        public static readonly Guid Account = new Guid("{229A6A53-B428-4FFB-A835-E8F36B5B4B1E}");
        public const string AccountResource = "Accounts";

        public static readonly Guid AccountUserId = new Guid("{DFA3B963-C8BB-4CAF-BCAC-5C066B3B5793}");
        public const string AccountUserResource = "Users";

        public const string HostMappingsResource = "HostMappings";
        public static readonly Guid HostsLocationid = new Guid("{DC2B7A91-2350-487B-9192-8099F28D6576}");

        public static readonly Guid AccountTenantId = new Guid("{C58B3989-1E17-4A18-9925-67186FE66833}");
        public const string AccountTenantResource = "Tenant";

        public static readonly Guid AccountRegionLocationId = new Guid("642A93C7-8385-4D63-A5A5-20D044FE504F");
        public const string AccountRegionResource = "Regions";

        public static readonly Guid AccountNameAvailabilityid = new Guid("65DD1DC5-53FE-4C67-9B4E-0EC3E2539998");
        public const string AccountNameAvailabilityResource = "Availability";

        public static readonly Guid AccountSettingsid = new Guid("4E012DD4-F8E1-485D-9BB3-C50D83C5B71B");
        public const string AccountSettingsResource = "Settings";
    }
}

namespace GitHub.Services.ClientNotification
{
    public static class ClientNotificationResourceIds
    {
        public const string AreaId = "C2845FF0-342A-4059-A831-AA7A5BF00FF0";
        public const string AreaName = "ClientNotification";

        public const string SubscriptionsResource = "Subscriptions";
        public static readonly Guid SubscriptionsLocationid = new Guid("E037C69C-5AD1-4B26-B340-51C18035516F");

        public const string NotificationsResource = "Notifications";
        public static readonly Guid NotificationsLocationid = new Guid("7F325780-EAD9-4C90-ACD1-2ECF621CE348");
    }
}

namespace GitHub.Services.Licensing
{
    public static class LicensingResourceIds
    {
        public const string AreaId = "C73A23A1-59BB-458C-8CE3-02C83215E015";
        public const string AreaName = "Licensing";

        public const string CertificateResource = "Certificate";
        public static readonly Guid CertificateLocationid = new Guid("2E0DBCE7-A327-4BC0-A291-056139393F6D");

        public const string ClientRightsResource = "ClientRights";
        public static readonly Guid ClientRightsLocationid = new Guid("643C72DA-EAEE-4163-9F07-D748EF5C2A0C");

        public const string MsdnResource = "Msdn";
        public const string MsdnPresenceLocationIdString = "69522C3F-EECC-48D0-B333-F69FFB8FA6CC";
        public static readonly Guid MsdnPresenceLocationId = new Guid(MsdnPresenceLocationIdString);
        public const string MsdnEntitlementsLocationIdString = "1cc6137e-12d5-4d44-a4f2-765006c9e85d";
        public static readonly Guid MsdnEntitlementsLocationId = new Guid(MsdnEntitlementsLocationIdString);

        public const string ExtensionRightsResource = "ExtensionRights";
        public static readonly Guid ExtensionRightsLocationId = new Guid("5F1DBE21-F748-47C7-B5FD-3770C8BC2C08");

        public const string ExtensionLicenseResource = "ExtensionRegistration";
        public static readonly Guid ExtensionLicenseLocationId = new Guid("004A420A-7BEF-4B7F-8A50-22975D2067CC");

        public const string UsageRightsResource = "UsageRights";
        public static readonly Guid UsageRightsLocationid = new Guid("D09AC573-58FE-4948-AF97-793DB40A7E16");

        public const string ServiceRightsResource = "ServiceRights";
        public static readonly Guid ServiceRightsLocationid = new Guid("78ED2F48-D449-412D-8772-E4E97317B7BE");

        public const string UsageResource = "Usage";
        public static readonly Guid UsageLocationid = new Guid("D3266B87-D395-4E91-97A5-0215B81A0B7D");

        public const string EntitlementsResource = "Entitlements";
        public const string EntitlementsBatchResource = "EntitlementsBatch";
        public const string ExtensionsAssignedToAccountResource = "AccountAssignedExtensions";
        public const string ExtensionsAssignedToAccountLocationIdString = "01BCE8D3-C130-480F-A332-474AE3F6662E";
        public static readonly Guid ExtensionsAssignedToAccountLocationId = new Guid(ExtensionsAssignedToAccountLocationIdString);

        public const string EntitlementsLocationIdString = "EA37BE6F-8CD7-48DD-983D-2B72D6E3DA0F";
        public static readonly Guid EntitlementsLocationid = new Guid(EntitlementsLocationIdString);
        public const string UserEntitlementsLocationIdString = "6490E566-B299-49A7-A4E4-28749752581F";
        public static readonly Guid UserEntitlementsLocationId = new Guid(UserEntitlementsLocationIdString);

        public const string UserEntitlementsBatchLocationIdString = "CC3A0130-78AD-4A00-B1CA-49BEF42F4656";
        public static readonly Guid UserEntitlementsBatchLocationId = new Guid(UserEntitlementsBatchLocationIdString);

        public const string CurrentUserEntitlementsLocationIdString = "C01E9FD5-0D8C-4D5E-9A68-734BD8DA6A38";
        public static readonly Guid CurrentUserEntitlementsLocationId = new Guid(CurrentUserEntitlementsLocationIdString);
        public const string AssignAvailableEntitlementsLocationIdString = "C01E9FD5-0D8C-4D5E-9A68-734BD8DA6A38";
        public static readonly Guid AssignAvailableEntitlementsLocationId = new Guid(AssignAvailableEntitlementsLocationIdString);

        public const string ExtensionEntitlementsResource = "ExtensionEntitlements";
        public const string UserExtensionEntitlementsLocationIdString = "8CEC75EA-044F-4245-AB0D-A82DAFCC85EA";
        public static readonly Guid UserExtensionEntitlementsLocationId = new Guid(UserExtensionEntitlementsLocationIdString);
        public const string ExtensionEntitlementsLocationIdString = "5434F182-7F32-4135-8326-9340D887C08A";
        public static readonly Guid ExtensionEntitlementsLocationId = new Guid(ExtensionEntitlementsLocationIdString);

        public const string TransferIdentitiesExtensionsResource = "TransferIdentitiesExtensions";
        public const string TransferIdentitiesExtensionsLocationIdString = "DA46FE26-DBB6-41D9-9D6B-86BF47E4E444";
        public static readonly Guid TransferIdentitiesExtensionsLocationId =
            new Guid(TransferIdentitiesExtensionsLocationIdString);

        public const string ExtensionEntitlementsBatchResource = "ExtensionEntitlementsBatch";
        public const string UsersBatchExtensionEntitlementsLocationIdString = "1D42DDC2-3E7D-4DAA-A0EB-E12C1DBD7C72";
        public static readonly Guid UsersBatchExtensionEntitlementsLocationId = new Guid(UsersBatchExtensionEntitlementsLocationIdString);

        public const string LicensingRightsResource = "LicensingRights";
        public const string LicensingRightsLocationIdString = "8671B016-FA74-4C88-B693-83BBB88C2264";
        public static readonly Guid LicensingRightsLocationId = new Guid(LicensingRightsLocationIdString);

        public const string LicensingSettingsResource = "Settings";
        public const string LicensingSettingsLocationIdString = "6BA7740F-A387-4D74-B71A-969A9F2B49FB";
        public static readonly Guid LicensingSettingsLocationId = new Guid(LicensingSettingsLocationIdString);
    }

    public static class LicensingResourceVersions
    {
        public const int AccountRightsResourcePreviewVersion = 1;
        public const int CertificateResourcePreviewVersion = 1;
        public const int ClientRightsResourcePreviewVersion = 1;
        public const int UsageRightsResourcePreviewVersion = 1;
        public const int ServiceRightsResourceRtmVersion = 1;
        public const int AccountUsageResourceRtmVersion = 1;
        public const int EntitlementResourceRtmVersion = 1;
        public const int EntitlementsBatchResourcePreviewVersion = 1;
        public const int LicensingRightsResourceRtmVersion = 1;
        public const int MsdnResourceRtmVersion = 1;
        public const int ExtensionRightsResourceRtmVersion = 1;
        public const int ExtensionLicenseResourceRtmVersion = 1;
        public const int ExtensionEntitlementsResourceRtmVersion = 1;
        public const int ExtensionEntitlementsBatchResourceRtmVersion = 1;
        public const int ExtensionEntitlementsBatch2ResourceRtmVersion = 2;
        public const int TransferExtensionsForIdentitiesRtmVersion = 1;
        public const int LicensingSettingsResourceRtmVersion = 1;
    }
}

namespace GitHub.Services.GroupLicensingRule
{
    public static class LicensingRuleResourceIds
    {
        public const string AreaId = "4F9A6C65-A750-4DE3-96D3-E4BCCF3A39B0";
        public const string AreaName = "LicensingRule";

        public static class GroupLicensingRules
        {
            public const string GroupLicensingRulesResourceName = "GroupLicensingRules";
            public const string GroupLicensingRuleLocationIdString = "1DAE9AF4-C85D-411B-B0C1-A46AFAEA1986";
            public static readonly Guid GroupLicensingRuleLocationId = new Guid(GroupLicensingRuleLocationIdString);
        }

        public static class GroupLicensingRulesLookup
        {
            public const string GroupLicensingRulesLookupResourceName = "GroupLicensingRulesLookup";
            public const string GroupLicensingRulesLookupResourceLocationIdString = "6282B958-792B-4F26-B5C8-6D035E02289F";
            public static readonly Guid GroupLicensingRulesLookupResourceLocationId = new Guid(GroupLicensingRulesLookupResourceLocationIdString);
        }

        public static class GroupLicensingRulesUserApplication
        {
            public const string GroupLicensingRulesUserApplicationResourceName = "GroupLicensingRulesUserApplication";
            public const string GroupLicensingRulesUserApplicationResourceLocationIdString = "74A9DE62-9AFC-4A60-A6D9-F7C65E028619";
            public static readonly Guid GroupLicensingRulesUserApplicationResourceLocationId = new Guid(GroupLicensingRulesUserApplicationResourceLocationIdString);
        }

        public static class GroupLicensingRulesApplication
        {
            public const string GroupLicensingRulesApplicationResourceName = "GroupLicensingRulesApplication";
            public const string GroupLicensingRulesApplicationResourceLocationIdString = "14602853-288e-4711-a613-c3f27ffce285";
            public static readonly Guid GroupLicensingRulesApplicationResourceLocationId = new Guid(GroupLicensingRulesApplicationResourceLocationIdString);
        }
        public static class GroupLicensingRulesApplicationStatus
        {
            public const string GroupLicensingRulesApplicationStatusResourceName = "GroupLicensingRulesApplicationStatus";
            public const string GroupLicensingRulesApplicationStatusResourceLocationIdString = "8953c613-d07f-43d3-a7bd-e9b66f960839";
            public static readonly Guid GroupLicensingRulesApplicationStatusResourceLocationId = new Guid(GroupLicensingRulesApplicationStatusResourceLocationIdString);
        }

        public static class GroupLicensingRulesEvaluationLog
        {
            public const string GroupLicensingRulesEvaluationLogResourceName = "GroupLicensingRulesEvaluationLog";
            public const string GroupLicensingRulesEvaluationLogResourceLocationIdString = "C3C87024-5143-4631-94CE-CB2338B04BBC";
            public static readonly Guid GroupLicensingRulesEvaluationLogResourceLocationId = new Guid(GroupLicensingRulesEvaluationLogResourceLocationIdString);
        }
    }

    public static class GroupLicensingResourceVersions
    {
        public const int GroupLicensingRulesResourceVersion = 1;
        public const int GroupLicensingRulesLookupResourceVersion = 1;
    }
}

namespace GitHub.Services.Invitation
{
    public static class InvitationResourceVersions
    {
        public const int InvitationAPIVersion = 1;
    }

    public static class InvitationResourceIds
    {
        public const string AreaId = "287A6D53-7DC8-4618-8D57-6945B848A4AD";
        public const string AreaName = "Invitation";

        public const string InvitationsResourceName = "Invitations";
        public const string InvitationsLocationIdString = "BC7CA053-E204-435B-A143-6240BA8A93BF";
        public static readonly Guid InvitationsLocationId = new Guid(InvitationsLocationIdString);
    }
}

namespace GitHub.Services.Compliance
{
    public static class ComplianceResourceVersions
    {
        public const int AccountRightsResourceVersion = 1;
        public const int ConfigurationResourceVersion = 1;
        public const int ValidationResourceVersion = 1;
    }

    public static class ComplianceResourceIds
    {
        public const string AreaId = "7E7BAADD-B7D6-46A0-9CE5-A6F95DDA0E62";
        public const string AreaName = "Compliance";

        public const string AccountRightsResource = "AccountRights";
        public static readonly Guid AccountRightsLocationId = new Guid("5FCEC4F4-491A-473D-B2F9-205977E66F01");

        public const string ConfigurationResource = "Configuration";
        public static readonly Guid ConfigurationLocationId = new Guid("64076419-AC67-4F85-B709-B8C28D5B4F1D");

        public const string ValidationResource = "Validation";
        public static readonly Guid ValidationLocationId = new Guid("A9994840-76C7-4C5B-97CF-2B353AD0E01C");
    }
}

namespace GitHub.Services.Profile
{
    public static class ProfileResourceIds
    {
        public const string AreaId = "8CCFEF3D-2B87-4E99-8CCB-66E343D2DAA8";
        public const string AreaName = "Profile";
        public static readonly Guid AreaIdGuid = new Guid(AreaId);

        public const string ProfileHttpClientV2AreaId = "31C4AD39-B95A-4AC2-87FE-D8CE878D32A8";

        public const string ProfileResource = "Profiles";
        public static readonly Guid ProfileLocationid = new Guid("F83735DC-483F-4238-A291-D45F6080A9AF");

        public const string UserDefaultsResource = "UserDefaults";
        public static readonly Guid UserDefaultsLocationId = new Guid("B583A356-1DA7-4237-9F4C-1DEB2EDBC7E8");

        public const string AttributeResource = "Attributes";
        public static readonly Guid AttributeLocationid = new Guid("EF743E8C-9A94-4E55-9392-CCFE55CFAE55");
        public static readonly Guid AttributeLocationId2 = new Guid("1392B6AC-D511-492E-AF5B-2263E5545A5D");

        public const string AvatarResource = "Avatar";
        public static readonly Guid AvatarLocationid = new Guid("855C48A5-ED0C-4762-A640-3D212B2244B8");
        public static readonly Guid Avatar2LocationId = new Guid("67436615-B382-462A-B659-5367A492FB3C");

        public const string DisplayNameResource = "DisplayName";
        public static readonly Guid DisplayNameLocationid = new Guid("5D969C0D-9A4A-45AB-A4EA-0C902AF8D39C");

        public const string PublicAliasResource = "PublicAlias";
        public static readonly Guid PublicAliasLocationid = new Guid("B63E58B3-B830-40EA-A382-C198E6E9BB2C");

        public const string EmailAddressResource = "EmailAddress";
        public static readonly Guid EmailAddressLocationid = new Guid("F47E1E09-08B3-436F-A541-495B3088635A");

        public const string CountryResource = "Country";
        public static readonly Guid CountryLocationid = new Guid("C96428D6-5805-48A4-B4FD-DC6F1C39BE92");

        public const string TermsOfServiceResource = "TermsOfService";
        public static readonly Guid TermsOfServiceLocationid = new Guid("E3411396-DA5F-4757-AA9E-521B48EEF625");

        public const string PreferredEmailConfirmationResource = "PreferredEmailConfirmation";
        public static readonly Guid PreferredEmailConfirmationLocationid = new Guid("238437E4-73B9-4BB9-B467-DE4E5DC0FC78");

        public const string CountriesResource = "Countries";
        public static readonly Guid CountriesLocationid = new Guid("775F46ED-26B3-4A6F-B7B1-01CF195ACDD0");

        public const string SupportedLcidsResource = "SupportedLcids";
        public static readonly Guid SupportedLcidsLocationId = new Guid("D5BD1AA6-C269-4BCD-AD32-75FA17475584");

        public const string RegionsResource = "Regions";
        public static readonly Guid RegionsLocationId = new Guid("92D8D1C9-26B8-4774-A929-D640A73DA524");

        public const string LocationsResource = "Locations";
        public static readonly Guid LocationsLocationid = new Guid("EEA7DE6F-00A4-42F3-8A29-1BA615691880");

        public const string LatestTosResource = "LatestTermsofService";
        public static readonly Guid LatestTosLocationid = new Guid("A4A9FB9D-FD32-4F9A-95A8-4B05FAF8C661");

        public const string SettingsResource = "Settings";
        public static readonly Guid SettingsLocationid = new Guid("5081DFF5-947B-4CE6-9BBE-6C7C094DDCE0");

        public const string GeoRegionResource = "GeoRegion";
        public static readonly Guid GeoRegionLocationid = new Guid("3BCDA9C0-3078-48A5-A1E0-83BD05931AD0");

        public const string MigratingProfilesResource = "MigratingProfiles";
        public static readonly Guid MigratingProfilesLocationid = new Guid("397E8E6D-00BB-405F-90F4-02B38B2AC8F6");
    }

    public static class ProfileResourceVersions
    {
        public const int GenericResourcePreviewVersion = 1;

        public const int ProfileResourcePreviewVersion = 1;
        public const int ProfileResourceRcVersion = 2;
        public const int ProfileResourceRtmVersion = 3;

        public const int AttributeResourcePreviewVersion = 1;
        public const int AttributeResourceRcVersion = 2;
    }
}

namespace GitHub.Services.FileContainer
{
    public static class FileContainerResourceIds
    {
        public const string FileContainerServiceArea = "Container";
        public const string FileContainerIdString = "E4F5C81E-E250-447B-9FEF-BD48471BEA5E";
        public const string BrowseFileContainerIdString = "E71A64AC-B2B5-4230-A4C0-DAD657CF97E2";

        public static readonly Guid FileContainer = new Guid(FileContainerIdString);
        public static readonly Guid BrowseFileContainer = new Guid(BrowseFileContainerIdString);

        public const string FileContainerResource = "Containers";
    }
}

namespace GitHub.Services.WebApi
{
    public static class CvsFileDownloadResourceIds
    {
        public const string AreaName = "CvsFileDownload";

        public const string LocationIdString = "0CF03C5A-D16D-4297-BFEB-F38A56D86670";
        public static readonly Guid LocationId = new Guid(LocationIdString);

        public const string Resource = "CvsFileDownload";
    }
}

namespace GitHub.Services.Commerce
{
    public static class CommerceResourceIds
    {
        public const string AreaId = "365D9DCD-4492-4AE3-B5BA-AD0FF4AB74B3";
        public const string AreaName = "Commerce";

        public const string MeterResource = "Meters";
        public static readonly Guid MeterLocationid = new Guid("AFB09D56-7740-4EB0-867F-792021FAB7C9");

        public const string CommercePackageResource = "CommercePackage";
        public static readonly Guid CommercePackageLocationId = new Guid("E8135F49-A1DC-4135-80F4-120BBFC2ACF0");

        public const string UsageEventResource = "UsageEvents";
        public static readonly Guid UsageEventLocationid = new Guid("EED7D28A-12A9-47ED-9A85-91A76C63E74B");

        public const string ReportingEventResource = "ReportingEvents";
        public static readonly Guid ReportingEventLocationId = new Guid("E3296A33-647F-4A09-85C6-64B9259DADB8");

        public const string SubscriptionResource = "Subscription";
        public static readonly Guid SubscriptionLocationId = new Guid("64485509-D692-4B70-B440-D02B3B809820");

        public const string RegionsResource = "Regions";
        public static readonly Guid RegionsLocationId = new Guid("9527c79d-9f3e-465d-8178-069106c39457");

        public const string OfferSubscriptionResource = "OfferSubscription";
        public static readonly Guid OfferSubscriptionResourceId = new Guid("E8950CE5-80BC-421F-B093-033C18FD3D79");

        public const string OfferMeterResource = "OfferMeter";
        public static readonly Guid OfferMeterLocationId = new Guid("8B79E1FB-777B-4D0A-9D2E-6A4B2B8761B9");

        public const string OfferMeterPriceResource = "OfferMeterPrice";
        public static readonly Guid OfferMeterPriceLocationId = new Guid("1C67C343-2269-4608-BC53-FE62DAA8E32B");

        public const string ConnectedServerResource = "ConnectedServer";
        public static readonly Guid ConnectedServerLocationId = new Guid("C9928A7A-8102-4061-BDCE-B090068C0D2B");

        public const string PurchaseRequestResource = "PurchaseRequest";
        public static readonly Guid PurchaseRequestLocationId = new Guid("A349B796-BDDB-459E-8921-E1967672BE86");

        public const string ResourceMigrationResource = "ResourceMigration";
        public static readonly Guid ResourceMigrationLocationId = new Guid("2F11E604-83B2-4596-B3C6-242BAB102DA3");

        public const string CommerceHostHelperResource = "CommerceHostHelperResource";
        public static readonly Guid CommerceHostHelperLocationId = new Guid("8B4C702A-7449-4FEB-9B23-ADD4288DDA1A");
    }

    public static class CommerceResourceVersions
    {
        public const int MeterV1Resources = 1;

        public const int MeterV2Resources = 2;

        public const int BillingV1Resources = 1;

        public const int OfferMeterV1Resources = 1;

        public const int OfferMeterPriceV1Resources = 1;

        public const int CommercePackageV1Resources = 1;

        public const int ReportingV1Resources = 1;

        public const int PurchaseRequestV1Resources = 1;

        public const int ResourceMigrationV1Resources = 1;

        public const int InfrastructureOrganizationV1Resources = 1;
    }

    public static class CsmResourceIds
    {
        public const string AreaId = "B3705FD5-DC18-47FC-BB2F-7B0F19A70822";
        public const string AreaName = "Csm";

        public const string ExtensionResourceResource = "ExtensionResource";
        public static readonly Guid ExtensionResourceLocationId = new Guid("9cb405cb-4a72-4a50-ab6d-be1da1726c33");

        public const string ExtensionResourceGroupResource = "ExtensionResourceGroup";
        public static readonly Guid ExtensionResourceGroupLocationId = new Guid("a509d9a8-d23f-4e0f-a69f-ad52b248943b");

        public const string AccountResourceResource = "AccountResource";
        public static readonly Guid AccountResourceResourceLocationId = new Guid("5745408e-6e9e-49c7-92bf-62932c8df69d");

        public const string AccountResourceGroupResource = "AccountResourceGroup";
        public static readonly Guid AccountResourceGroupLocationId = new Guid("73d8b171-a2a0-4ac6-ba0b-ef762098e5ec");

        public const string SubscriptionResourceGroupResource = "SubscriptionResourceGroup";
        public static readonly Guid SubscriptionResourceGroupLocationId = new Guid("f34be62f-f215-4bda-8b57-9e8a7a5fd66a");

        public const string AccountResourceOperationsResource = "AccountResourceOperations";
        public static readonly Guid AccountResourceOperationsLocationId = new Guid("454d976b-812e-4947-bc4e-c2c23160317e");

        public const string NameAvailabilityResource = "NameAvailability";
        public static readonly Guid NameAvailabilityResourceLocationId = new Guid("031d6b9b-a0d4-4b46-97c5-9ddaca1aa5cd");

        public const string SubscriptionEventsResource = "SubscriptionEvents";
        public static readonly Guid SubscriptionEventsLocationId = new Guid("97bc4c4d-ce2e-4ca3-87cc-2bd07aeee500");

        public const string ResourceGroupsResourceName = "ResourceGroups";
        public static readonly Guid ResourceGroupsResourceLocationId = new Guid("9e0fa51b-9d61-4899-a5a1-e1f0f5e75bc0");
    }

    public static class CommerceServiceResourceIds
    {
        // Offer Meter Area
        public const string OfferMeterAreaId = "000080C1-AA68-4FCE-BBC5-C68D94BFF8BE";
        public const string OfferMeterAreaName = "OfferMeter";

        public const string OfferMeterLocationString = "81E37548-A9E0-49F9-8905-650A7260A440";
        public static readonly Guid OfferMeterLocationId = new Guid(OfferMeterLocationString);
        public const string OfferMeterResource = "OfferMeter";

        public const string OfferMeterPriceResource = "OfferMeterPrice";
        public const string OfferMeterPriceLocationString = "D7197E00-DDDF-4029-9F9B-21B935A6CF9F";
        public static readonly Guid OfferMeterPriceLocationId = new Guid(OfferMeterPriceLocationString);

        // Meters Area
        public const string MeterAreaId = "4C19F9C8-67BD-4C18-800B-55DC62C3017F";
        public const string MetersAreaName = "Meters";

        public const string MeterResource = "Meters";
        public const string MeterLocationString = "4BD6E06B-1EDF-41A6-9BAF-D15B874DC539";
        public static readonly Guid MeterLocationid = new Guid(MeterLocationString);

        // Commerce Package Area
        public const string CommercePackageAreaName = "Package";
        public const string CommercePackageAreaId = "45FB9450-A28D-476D-9B0F-FB4AEDDDFF73";

        public const string CommercePackageResource = "CommercePackage";
        public const string CommercePackageLocationString = "A5E80D85-9718-44E0-BBED-461109268DBC";
        public static readonly Guid CommercePackageLocationId = new Guid(CommercePackageLocationString);

        // Usage Events Area
        public const string UsageEventsAreaName = "UsageEvents";
        public const string UsageEventsAreaId = "3B16A4DB-B853-4C64-AA16-72138F5BB750";

        public const string UsageEventsResource = "UsageEvents";
        public const string UsageEventsLocationString = "78741F74-E4F0-41B2-BB93-28C886443027";
        public static readonly Guid UsageEventLocationid = new Guid(UsageEventsLocationString);

        // Reporting Event Area
        public const string ReportingEventsAreaName = "ReportingEvents";
        public const string ReportingEventsAreaId = "C890B7C4-5CF6-4280-91AC-331E439B8119";

        public const string ReportingEventsResource = "ReportingEvents";
        public const string ReportingEventsLocationString = "D0BA838F-9253-46C5-ABB2-0ACF551C23D7";
        public static readonly Guid ReportingEventsLocationId = new Guid(ReportingEventsLocationString);

        // Subscription Area
        public const string SubscriptionAreaId = "AC02550F-721A-4913-8EA5-CADAE535B03F";
        public const string SubscriptionAreaName = "Subscription";

        public const string SubscriptionResource = "Subscription";
        public const string SubscriptionLocationString = "94DE86A2-03E3-42DB-A2E8-1A82BF13A262";
        public static readonly Guid SubscriptionLocationId = new Guid(SubscriptionLocationString);

        public const string AccountDetailsResource = "AccountDetails";
        public const string AccountDetailsLocationString = "0288F4E6-21D3-4529-AC5F-1719F99A4396";
        public static readonly Guid AccountDetailsLocationId = new Guid(AccountDetailsLocationString);

        // Region Area
        public const string RegionsAreaName = "Regions";
        public const string RegionsAreaId = "A6ACEE79-C91A-47BA-87DF-AF36581833B6";

        public const string RegionsResource = "Regions";
        public const string RegionsLocationString = "AAE8A531-9968-456F-9EF1-FE0ECF4724E8";
        public static readonly Guid RegionsLocationId = new Guid(RegionsLocationString);

        // Offer Subscription Area
        public const string OfferSubscriptionAreaName = "OfferSubscription";
        public const string OfferSubscriptionAreaId = "5D4A2F52-5A08-41FB-8CCA-768ADD070E18";

        public const string OfferSubscriptionResource = "OfferSubscription";
        public const string OfferSubscriptionLocationString = "7C13D166-01C5-4CCD-8A75-E5AD6AB3B0A6";
        public static readonly Guid OfferSubscriptionResourceId = new Guid(OfferSubscriptionLocationString);

        // Connected Server Area
        public const string ConnectedServerAreaName = "ConnectedServer";
        public const string ConnectedServerAreaId = "05A2B228-317C-4886-9FE9-828F9EA3815A";

        public const string ConnectedServerResource = "ConnectedServer";
        public const string ConnectedServerLocationString = "AB6E0E2F-A3CA-4478-BAFC-8E7AD022BE01";
        public static readonly Guid ConnectedServerLocationId = new Guid(ConnectedServerLocationString);

        // Purchase Request Area
        public const string PurchaseRequestAreaName = "PurchaseRequest";
        public const string PurchaseRequestAreaId = "9D439667-F8CF-4991-89A9-95CA6A763327";

        public const string PurchaseRequestResource = "PurchaseRequest";
        public const string PurchaseRequestLocationString = "6F905B2D-292A-4D30-B38A-2D254EAB06B7";
        public static readonly Guid PurchaseRequestLocationId = new Guid(PurchaseRequestLocationString);

        // Resource Migration Area
        public const string ResourceMigrationAreaName = "ResourceMigration";
        public const string ResourceMigrationAreaId = "FFCFC36A-0BE8-412A-A2BB-93C2ABD4048B";

        public const string ResourceMigrationResource = "ResourceMigration";
        public const string ResourceMigrationLocationString = "00432895-B3F6-488C-BA71-792FA5E07383";
        public static readonly Guid ResourceMigrationLocationId = new Guid(ResourceMigrationLocationString);
    }

    public static class CsmResourceProviderResourceIds
    {
        public const string AreaId = "2900E97E-7BBD-4D87-95EE-BE54611B6184";
        public const string AreaName = "CsmResourceProvider";

        public const string ExtensionResourceResource = "VssExtensionResource";
        public static readonly Guid ExtensionResourceLocationId = new Guid("8DF1CB68-197E-4BAF-8CE2-C96021879971");

        public const string ExtensionResourceGroupResource = "VssExtensionResourceGroup";
        public static readonly Guid ExtensionResourceGroupLocationId = new Guid("E14787AB-FBD5-4064-A75D-0603C9ED66A8");

        public const string AccountResourceResource = "VssAccountResource";
        public static readonly Guid AccountResourceResourceLocationId = new Guid("58FA3A85-AF20-408D-B46D-6D369408E3DA");

        public const string AccountResourceGroupResource = "VssAccountResourceGroup";
        public static readonly Guid AccountResourceGroupLocationId = new Guid("955956A7-FBEB-48E6-9D78-C60F3F84BAE9");

        public const string SubscriptionResourceGroupResource = "VssSubscriptionResourceGroup";
        public static readonly Guid SubscriptionResourceGroupLocationId = new Guid("8A066194-3817-4E76-9BBC-2A1446FA0FC5");

        public const string AccountResourceOperationsResource = "VssAccountResourceOperations";
        public static readonly Guid AccountResourceOperationsLocationId = new Guid("14917175-ECBE-453B-B436-50430219EBA9");

        public const string NameAvailabilityResource = "VssNameAvailability";
        public static readonly Guid NameAvailabilityResourceLocationId = new Guid("7DBAE6E1-993E-4AC9-B20D-6A39EEE4028B");

        public const string SubscriptionEventsResource = "VssSubscriptionEvents";
        public static readonly Guid SubscriptionEventsLocationId = new Guid("A7F5BE2F-9AF8-4CC2-863F-D07377B2C079");

        public const string ResourceGroupsResource = "VssResourceGroups";
        public static readonly Guid ResourceGroupsResourceLocationId = new Guid("8D9245EE-19A2-45B2-BE3E-03234122298E");
    }
}

namespace GitHub.Services.Health
{
    public static class HealthResourceIds
    {
        public const string HealthArea = "Health";
        public const string HealthResource = "Health";
        public static readonly Guid HealthLocationId = new Guid("30964BA7-2A11-4792-B7BA-DF191DBCC3BB");
    }
}

namespace GitHub.Services.ActivityStatistic
{
    public static class ActivityStatisticIds
    {
        public const string ActivityStatisticArea = "Stats";
        public const string ActivityStatisticResource = "Activities";
        public static readonly Guid ActivityStatisticId = new Guid("5F4C431A-4D8F-442D-96E7-1E7522E6EABD");
    }
}

namespace GitHub.Services.ContentSecurityPolicy
{
    public static class ContentSecurityPolicyResourceIds
    {
        public const string CspReportArea = "CspReport";
        public const string CspReportResource = "CspReport";
        public static readonly Guid CspLocationId = new Guid("FA48A6B6-C4A9-42B4-AFE7-2640F68F99B6");
    }
}

namespace GitHub.Services.Location
{
    [GenerateAllConstants]
    public static class LocationResourceIds
    {
        public const string LocationServiceArea = "Location";

        public const string ConnectionDataResource = "ConnectionData";
        public static readonly Guid ConnectionData = new Guid("{00D9565F-ED9C-4A06-9A50-00E7896CCAB4}");

        public const string ServiceDefinitionsResource = "ServiceDefinitions";
        public static readonly Guid ServiceDefinitions = new Guid("{D810A47D-F4F4-4A62-A03F-FA1860585C4C}");

        public const string AccessMappingsResource = "AccessMappings";
        public static readonly Guid AccessMappings = new Guid("{A52F2F69-B171-4E88-9DFE-34B44CF7E386}");

        public const string ResourceAreasResource = "ResourceAreas";
        public static readonly Guid ResourceAreas = new Guid("E81700F7-3BE2-46DE-8624-2EB35882FCAA");

        // Used for updating the SPS locations in account migrations.
        public const string SpsServiceDefintionResource = "SpsServiceDefinition";

        public static readonly Guid SpsServiceDefinition = new Guid("{DF5F298A-4E06-4815-A13E-6CE90A37EFA4}");
    }
}

namespace GitHub.Services.Notification
{
    public static class PersistedNotificationResourceIds
    {
        public const string AreaId = "BA8495F8-E9EE-4A9E-9CBE-142897543FE9";
        public const string AreaName = "PersistedNotification";

        public static readonly Guid NotificationsId = new Guid("E889FFCE-9F0A-4C6C-B749-7FB1ECFA6950");
        public const string NotificationsResource = "Notifications";

        public static readonly Guid RecipientMetadataId = new Guid("1AAFF2D2-E2F9-4784-9F93-412A9F2EFD86");
        public const string RecipientMetadataResource = "RecipientMetadata";
    }

    public static class PersistedNotificationResourceVersions
    {
        public const int NotificationsResourcePreviewVersion = 1;
        public const int RecipientMetadataPreviewVersion = 1;
    }
}

namespace GitHub.Services.Operations
{
    [GenerateAllConstants]
    public static class OperationsResourceIds
    {
        public const string AreaName = "operations";
        public const string OperationsResource = "operations";
        public const string OperationsRouteName = "Operations";
        public const string OperationsPluginRouteName = "OperationsPlugin";
        public const string OperationsApi = "OperationsApi";
        public const string TagOperationsLocationId = "9A1B74B4-2CA8-4A9F-8470-C2F2E6FDC949";
        public static readonly Guid OperationsLocationId = new Guid(TagOperationsLocationId);
        public const string TagOperationsPluginLocationId = "7F82DF6D-7D09-46C1-A015-643B556B3A1E";
        public static readonly Guid OperationsPluginLocationId = new Guid(TagOperationsPluginLocationId);
    }
}

namespace GitHub.Services.Directories.DirectoryService
{
    public static class DirectoryResourceIds
    {
        public const string DirectoryServiceArea = "Directory";
        public const string DirectoryService = "2B98ABE4-FAE0-4B7F-8562-7141C309B9EE";

        public const string MembersResource = "Members";
        public static readonly Guid Members = Guid.Parse("{89526A2C-E9E3-1F40-A3FB-54D16BDA15B0}");
        public static readonly Guid MemberStatusLocationId = Guid.Parse("{714914b2-ad3f-4933-bf2e-fc3cabb37696}");
    }
}

namespace GitHub.Services.FeatureAvailability
{
    [GenerateAllConstants]
    public static class FeatureAvailabilityResourceIds
    {
        public const string AreaId = "C8E5AF97-4B95-4E73-9E7F-69A06507967C";
        public const string FeatureAvailabilityAreaName = "FeatureAvailability";
        public static readonly Guid FeatureFlagsLocationId = Guid.Parse("{3E2B80F8-9E6F-441E-8393-005610692D9C}");
    }
}

namespace GitHub.Services.IdentityPicker
{
    //Common identity picker in the framework
    [GenerateAllConstants]
    public static class CommonIdentityPickerResourceIds
    {
        public const string ServiceArea = "IdentityPicker";

        public const string IdentitiesResource = "Identities";

        public static readonly Guid IdentitiesLocationId = new Guid("4102F006-0B23-4B26-BB1B-B661605E6B33");
        public static readonly Guid IdentityAvatarLocationId = new Guid("4D9B6936-E96A-4A42-8C3B-81E8337CD010");
        public static readonly Guid IdentityFeatureMruLocationId = new Guid("839E4258-F559-421B-A38E-B6E691967AB3");
        public static readonly Guid IdentityConnectionsLocationId = new Guid("C01AF8FD-2A61-4811-A7A3-B85BCEC080AF");
    }
}

namespace GitHub.Services.Settings
{
    [GenerateAllConstants]
    public static class SettingsApiResourceIds
    {
        public const string SettingsAreaName = "Settings";

        public const string SettingEntriesResource = "Entries";
        public const string SettingEntriesLocationIdString = "CD006711-163D-4CD4-A597-B05BAD2556FF";
        public static readonly Guid SettingEntriesLocationId = new Guid(SettingEntriesLocationIdString);
        public const string NamedScopeSettingEntriesLocationIdString = "4CBAAFAF-E8AF-4570-98D1-79EE99C56327";
        public static readonly Guid NamedScopeSettingEntriesLocationId = new Guid(NamedScopeSettingEntriesLocationIdString);
    }
}

namespace GitHub.Services.WebPlatform
{
    [GenerateAllConstants]
    public static class AuthenticationResourceIds
    {
        public const string AreaId = "A084B81B-0F23-4136-BAEA-98E07F3C7446";
        public const string AuthenticationAreaName = "WebPlatformAuth";
        public static readonly Guid AuthenticationLocationId = Guid.Parse("{11420B6B-3324-490A-848D-B8AAFDB906BA}");
        public const string SessionTokenResource = "SessionToken";
    }

    [GenerateAllConstants]
    public static class CustomerIntelligenceResourceIds
    {
        public const string AreaId = "40132BEE-F5F3-4F39-847F-80CC44AD9ADD";
        public const string CustomerIntelligenceAreaName = "CustomerIntelligence";
        public static readonly Guid EventsLocationId = Guid.Parse("{B5CC35C2-FF2B-491D-A085-24B6E9F396FD}");
    }

    public static class ContributionResourceIds
    {
        public const string AreaId = "39675476-C858-48A1-A5CD-80ED65E86532";
        public const string AreaName = "Contribution";
        public const string HierarchyLocationIdString = "8EC9F10C-AB9F-4618-8817-48F3125DDE6A";
        public static readonly Guid HierarchyLocationId = Guid.Parse(HierarchyLocationIdString);
        public const string HierarchyResource = "Hierarchy";
        public const string HierarchyQueryLocationIdString = "3353E165-A11E-43AA-9D88-14F2BB09B6D9";
        public static readonly Guid HierarchyQueryLocationId = Guid.Parse(HierarchyQueryLocationIdString);
        public const string HierarchyQueryResource = "HierarchyQuery";
    }

    [GenerateAllConstants]
    public static class ClientTraceResourceIds
    {
        public const string AreaId = "054EEB0E-108E-47DC-848A-7074B14774A9";
        public const string ClientTraceAreaName = "ClientTrace";
        public static readonly Guid EventsLocationId = Guid.Parse("{06BCC74A-1491-4EB8-A0EB-704778F9D041}");
        public const string ClientTraceEventsResource = "Events";
    }
}

namespace GitHub.Services.Zeus
{
    [GenerateAllConstants]
    public static class BlobCopyLocationIds
    {
        public const string ResourceString = "{8907fe1c-346a-455b-9ab9-dde883687231}";
        public static readonly Guid ResourceId = new Guid(ResourceString);
        public const string ResouceName = "BlobCopyRequest";
        public const string AreaName = "BlobCopyRequest";
    }

    [GenerateAllConstants]
    public static class DatabaseMigrationLocationIds
    {
        public const string ResourceString = "{D56223DF-8CCD-45C9-89B4-EDDF69240000}";
        public static readonly Guid ResourceId = new Guid(ResourceString);
        public const string ResouceName = "DatabaseMigration";
        public const string AreaName = "DatabaseMigration";
    }
}

namespace GitHub.Services.Identity.Mru
{
    [GenerateAllConstants]
    public static class IdentityMruResourceIds
    {
        public const string AreaId = "FC3682BE-3D6C-427A-87C8-E527B16A1D05";
        public const string AreaName = "Identity";

        public static readonly Guid MruIdentitiesLocationId = new Guid("15D952A1-BB4E-436C-88CA-CFE1E9FF3331");
        public const string MruIdentitiesResource = "MruIdentities";
    }
}

namespace GitHub.Services.Servicing
{
    public static class ServicingResourceIds
    {
        public const string AreaName = "Servicing";

        public static readonly Guid JobsLocationId = new Guid("807F536E-0C6D-46D9-B856-4D5F3C27BEF5");
        public static readonly Guid LogsLocationId = new Guid("B46254F3-9523-4EF8-B69E-FD6EED5D0BB8");
        public static readonly Guid ServiceLevelLocationId = new Guid("3C4BFE05-AEB6-45F8-93A6-929468401657");

        public const string JobsResourceName = "Jobs";
        public const string LogsResourceName = "Logs";
        public const string ServiceLevelResource = "ServiceLevel";
    }
}

namespace GitHub.Services.Auditing
{
    public static class AuditingResourceIds
    {
        public const string AreaName = "Auditing";

        public static readonly Guid EndpointsLocationId = new Guid("D4AB3CD0-66BE-4551-844E-CC2C32FA64C5");
        public const string EndpointResourceName = "Endpoints";
    }
}

namespace GitHub.Services.ServicePrincipal
{
    public static class ServicePrincipalResourceIds
    {
        public const string AreaName = "ServicePrincipal";
        public const string ServicePrincipalsResourceName = "ServicePrincipals";
        public static readonly Guid ServicePrincipalsLocationId = new Guid("992CB93B-847E-4683-88C9-848CD450FDF6");
    }
}

namespace GitHub.Services.TokenSigningKeyLifecycle
{
    public static class TokenSigningKeyLifecycleResourceIds
    {
        public const string AreaName = "TokenSigning";
        public const string AreaId = "{f189ca86-04a2-413c-81a0-abdbd7c472da}";

        public const string SigningKeysResourceName = "SigningKeys";
        public static readonly Guid SigningKeysLocationId = new Guid("62361140-9bb7-4d57-8223-12e6155ce354");

        public const string NamespaceResourceName = "SigningNamespace";
        public static readonly Guid NamespaceLocationId = new Guid("29f94429-6088-4394-afd9-0435df55f079");
    }
}

namespace GitHub.Services.GitHubConnector
{
    public static class GitHubConnectorResourceIds
    {
        public const string AreaId = "85738938-9FAE-4EB4-B4F0-871502E6B549";
        public const string AreaName = "GitHubConnector";

        public static readonly Guid ResourceAreaId = new Guid(AreaId);

        public static readonly Guid ConnectionsResourceLocationId = new Guid("EBE1CF27-8F19-4955-A47B-09F125F06518");
        public const string ConnectionsResourceName = "Connections";

        public static readonly Guid InstallationTokensResourceLocationId = new Guid("05188D9F-DD80-4C9E-BA91-4B0B3A8A67D7");
        public const string InstallationTokensResourceName = "InstallationTokens";

        public static readonly Guid WebhookEventsResourceLocationId = new Guid("063EC204-5C0D-402F-86CF-36B1703E187F");
        public const string WebhookEventsResourceName = "WebhookEvents";

        public static readonly Guid UserOAuthUrlsResourceLocationId = new Guid("9EA35039-A91F-4E02-A81D-573623FF7235");
        public const string UserOAuthUrlsResourceName = "UserOAuthUrls";

        public const string DefaultResourceId = "default";
    }
}

namespace GitHub.Services.Organization
{
    public static class OrganizationResourceIds
    {
        public const string AreaId = "0D55247A-1C47-4462-9B1F-5E2125590EE6";

        public static readonly Guid ResourceAreaId = new Guid(AreaId);

        public const string OrganizationArea = "Organization";

        public const string PropertiesResourceName = "Properties";

        public const string LogoResourceName = "Logo";

        // organization resources
        public static readonly Guid OrganizationsResourceLocationId = new Guid("95F49097-6CDC-4AFE-A039-48B4D4C4CBF7");

        public const string OrganizationsResourceName = "Organizations";

        // organization properties resources
        public static readonly Guid OrganizationPropertiesResourceLocationId = new Guid("103707C6-236D-4434-A0A2-9031FBB65FA6");

        public const string OrganizationPropertiesResourceName = "OrganizationProperties";

        // organization logo resources
        public static readonly Guid OrganizationLogoResourceLocationId = new Guid("A9EEEC19-85B4-40AE-8A52-B4F697260AC4");

        public const string OrganizationLogoResourceName = "OrganizationLogo";

        // organization migration blobs resources
        public static readonly Guid OrganizationMigrationBlobsResourceLocationId = new Guid("93F69239-28BA-497E-B4D4-33E51E6303C3");

        public const string OrganizationMigrationBlobsResourceName = "OrganizationMigrationBlobs";

        // collection resources
        public static readonly Guid CollectionsResourceLocationId = new Guid("668B5607-0DB2-49BB-83F8-5F46F1094250");

        public const string CollectionsResourceName = "Collections";

        // collection properties resources
        public static readonly Guid CollectionPropertiesResourceLocationId = new Guid("A0F9C508-A3C4-456B-A812-3FB0C4743521");

        public const string CollectionPropertiesResourceName = "CollectionProperties";

        // region resources
        public static readonly Guid RegionsResourceLocationId = new Guid("6F84936F-1801-46F6-94FA-1817545D366D");

        public const string RegionsResourceName = "Regions";
    }

    public static class OrganizationPolicyResourceIds
    {
        public const string OrganizationPolicyArea = "OrganizationPolicy";

        // policy
        public static readonly Guid PoliciesLocationId = new Guid("D0AB077B-1B97-4F78-984C-CFE2D248FC79");

        public const string PoliciesResourceName = "Policies";

        // policies batch
        public static readonly Guid PoliciesBatchLocationId = new Guid("7EF423E0-59D8-4C00-B951-7143B18BD97B");

        public const string PoliciesBatchResourceName = "PoliciesBatch";

        // policy metadata
        public static readonly Guid PolicyInformationLocationId = new Guid("222AF71B-7280-4A95-80E4-DCB0DEEAC834");

        public const string PolicyInformationResourceName = "PolicyInformation";
    }
}

namespace GitHub.Services.UserMapping
{
    public static class UserMappingResourceIds
    {
        public const string AreaId = "C8C8FFD0-2ECF-484A-B7E8-A226955EE7C8";
        public const string UserMappingArea = "UserMapping";

        public static readonly Guid UserAccountMappingsResourceLocationId = new Guid("0DBF02CC-5EC3-4250-A145-5BEB580E0086");
        public const string UserAccountMappingsResourceName = "UserAccountMappings";
    }
}

namespace GitHub.Services.TokenRevocation
{
    public static class TokenRevocationResourceIds
    {
        public const string AreaName = "TokenRevocation";
        public const string AreaId = "{3C25A612-6355-4A43-80FE-75AEBE07E981}";

        public const string RulesResourceName = "Rules";
        public static readonly Guid RulesLocationId = new Guid("03923358-D412-40BA-A63F-36A1836C7706");
    }
}

namespace GitHub.Services.MarketingPreferences
{
    public static class MarketingPreferencesResourceIds
    {
        public const string AreaId = "F4AA2205-FF00-4EEE-8216-C7A73CEE155C";
        public const string AreaName = "MarketingPreferences";

        public const string ContactWithOffersResource = "ContactWithOffers";
        public static readonly Guid ContactWithOffersLocationid = new Guid("6E529270-1F14-4E92-A11D-B496BBBA4ED7");

        public const string MarketingPreferencesResource = "MarketingPreferences";
        public static readonly Guid MarketingPreferencesLocationId = new Guid("0e2ebf6e-1b6c-423d-b207-06b1afdfe332");
    }

    public static class MarketingPreferencesResourceVersions
    {
        public const int GenericResourcePreviewVersion = 1;
    }
}

namespace GitHub.Services.HostAcquisition
{
    public static class HostAcquisitionResourceIds
    {
        public const string AreaName = "HostAcquisition";
        public const string AreaId = "8E128563-B59C-4A70-964C-A3BD7412183D";

        public static readonly Guid ResourceAreaId = new Guid(AreaId);

        public const string HostAcquisitionArea = "HostAcquisition";

        // collection resources
        public static readonly Guid CollectionsResourceLocationId = new Guid("2BBEAD06-CA34-4DD7-9FE2-148735723A0A");

        public const string CollectionsResourceName = "Collections";

        // NameAvailability resources
        public static readonly Guid NameAvailabilityResourceLocationId = new Guid("01A4CDA4-66D1-4F35-918A-212111EDC9A4");

        public const string NameAvailabilityResourceName = "NameAvailability";

        // region resources
        public static readonly Guid RegionsResourceLocationId = new Guid("776EF918-0DAD-4EB1-A614-04988CA3A072");

        public const string RegionsResourceName = "Regions";
    }
}

namespace GitHub.Services.OAuthWhitelist
{
    public static class OAuthWhitelistResourceIds
    {
        public const string AreaId = "BED1E9DD-AE97-4D73-9E01-4797F66ED0D3";
        public const string AreaName = "OAuthWhitelist";

        public const string OAuthWhitelistEntriesResource = "OAuthWhitelistEntries";
        public static readonly Guid OAuthWhitelistEntriesLocationId = new Guid("3AFD5B3F-12B1-4551-B6D7-B33E0E2D45D6");
    }
}

namespace GitHub.Services.CentralizedFeature
{
    public class CentralizedFeatureResourceIds
    {
        public const string AreaName = "CentralizedFeature";
        public const string AreaId = "86BF2186-3092-4F5E-86A6-13997CE0924A";
        public static readonly Guid AreaIdGuid = new Guid(AreaId);

        public class Availability
        {
            public static readonly Guid LocationId = new Guid("EB8B51A6-1BE5-4337-B4C1-BAE7BCB587C2");
            public const string Resource = "Availability";
        }
    }
}

namespace GitHub.Services.AzureFrontDoor
{
    public static class AfdResourceIds
    {
        public const string AreaName = "AzureFrontDoor";

        public const string AfdEndpointLookupResource = "AfdEndpointLookup";
        public static readonly Guid EndpointLookupLocationId = new Guid("39738637-F7C6-439A-82D7-83EFAA3A7DB4");
    }
}

namespace GitHub.Services.WebApi
{
    public static class BasicAuthBatchResourceIds
    {
        public const string AreaName = "BasicAuthBatch";
        public const string AreaId = "31D56A90-A194-4567-AACF-EFE0007E3309";

        public const string BasicAuthBatchResource = "BasicAuthBatch";
        public static readonly Guid BasicAuthBatch = new Guid("{8214680a-5c4a-4333-9b3c-228030c136f6}");
    }
}

namespace GitHub.Services.PermissionLevel
{
    public static class PermissionLevelDefinitionResourceIds
    {
        public const string AreaName = "PermissionLevel";
        public const string AreaId = "E97D4D3C-C339-4745-A987-BD6F6C496788";

        public static readonly Guid ResourceAreaId = new Guid(AreaId);

        public static readonly Guid PermissionLevelDefinitionsResourceLocationId = new Guid("D9247EA2-4E01-47C1-8662-980818AAE5D3");

        public const string PermissionLevelDefinitionsResourceName = "PermissionLevelDefinitions";        
    }

    public static class PermissionLevelAssignmentResourceIds
    {
        public const string AreaName = "PermissionLevel";
        public const string AreaId = "E97D4D3C-C339-4745-A987-BD6F6C496788";

        public static readonly Guid ResourceAreaId = new Guid(AreaId);
        public static readonly Guid PermissionLevelAssignmentsResourceLocationId = new Guid("005E0302-7988-4066-9AC0-1D93A42A9F0B");

        public const string PermissionLevelAssignmentsResourceName = "PermissionLevelAssignments";
    }
}
