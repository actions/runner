using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Security.Client
{
    public class SecurityBackingStoreHttpClient : VssHttpClientBase
    {
        public SecurityBackingStoreHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public SecurityBackingStoreHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public SecurityBackingStoreHttpClient(Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public SecurityBackingStoreHttpClient(Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public SecurityBackingStoreHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<SecurityNamespaceDataCollection> QuerySecurityDataAsync(
            Guid securityNamespaceId,
            bool useVsidSubjects = true,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "QuerySecurityData"))
            {
                List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();

                if (!useVsidSubjects)
                {
                    query.Add(QueryParameters.UseVsidSubjects, useVsidSubjects);
                }

                return await GetAsync<SecurityNamespaceDataCollection>(
                    queryParameters: query,
                    locationId: LocationResourceIds.SecurityBackingStoreNamespace,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: new ApiResourceVersion(new Version(3, 0), 3),
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<SecurityNamespaceData> QuerySecurityDataAsync(
            Guid securityNamespaceId,
            Guid aclStoreId,
            long oldSequenceId = -1,
            bool useVsidSubjects = true,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForEmptyGuid(aclStoreId, "aclStoreId");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "QuerySecurityData"))
            {
                List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();

                if (oldSequenceId >= 0)
                {
                    query.Add(QueryParameters.OldSequenceId, oldSequenceId.ToString());
                }

                if (!useVsidSubjects)
                {
                    query.Add(QueryParameters.UseVsidSubjects, useVsidSubjects);
                }

                return await GetAsync<SecurityNamespaceData>(
                    queryParameters: query,
                    locationId: LocationResourceIds.SecurityBackingStoreAclStore,
                    routeValues: new { securityNamespaceId = securityNamespaceId, aclStoreId = aclStoreId },
                    version: new ApiResourceVersion(new Version(3, 0), 2),
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<long> SetAccessControlListsAsync(
            Guid securityNamespaceId,
            IEnumerable<AccessControlList> acls,
            IEnumerable<AccessControlEntry> aces,
            bool throwOnInvalidIdentity = true,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForNull(acls, "acls");
            ArgumentUtility.CheckForNull(aces, "aces");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "SetAccessControlLists"))
            {
                return PostAsync<SetAccessControlListsRequest, long>(
                    value: new SetAccessControlListsRequest(acls, aces, throwOnInvalidIdentity),
                    locationId: LocationResourceIds.SecurityBackingStoreAcls,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: s_apiVersion1,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public Task<long> RemoveAccessControlListsAsync(
            Guid securityNamespaceId,
            IEnumerable<String> tokens,
            bool recurse,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForNull(tokens, "tokens");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "RemoveAccessControlLists"))
            {
                return PatchAsync<RemoveAccessControlListsRequest, long>(
                    value: new RemoveAccessControlListsRequest(tokens, recurse),
                    locationId: LocationResourceIds.SecurityBackingStoreAcls,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: s_apiVersion1,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public Task<long> SetPermissionsAsync(
            Guid securityNamespaceId,
            String token,
            IEnumerable<AccessControlEntry> permissions,
            bool merge,
            bool throwOnInvalidIdentity = true,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForNull(token, "token");
            ArgumentUtility.CheckForNull(permissions, "permissions");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "SetPermissions"))
            {
                return PostAsync<SetPermissionsRequest, long>(
                    value: new SetPermissionsRequest(token, permissions, merge, throwOnInvalidIdentity),
                    locationId: LocationResourceIds.SecurityBackingStoreAces,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: s_apiVersion1,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public Task<long> RemovePermissionsAsync(
            Guid securityNamespaceId,
            String token,
            IEnumerable<Guid> identityIds,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForNull(token, "token");
            ArgumentUtility.CheckForNull(identityIds, "identityIds");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "RemovePermissions"))
            {
                return PatchAsync<RemovePermissionsRequest, long>(
                    value: new RemovePermissionsRequest(token, identityIds),
                    locationId: LocationResourceIds.SecurityBackingStoreAces,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: s_apiVersion1,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public Task<long> SetInheritFlagAsync(
            Guid securityNamespaceId,
            String token,
            bool inheritFlag,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForNull(token, "token");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "SetInheritFlag"))
            {
                List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();
                query.Add(QueryParameters.Token, token);
                query.Add(QueryParameters.InheritFlag, inheritFlag.ToString().ToLowerInvariant());

                return this.SendAsync<long>(
                    method: new HttpMethod("PATCH"),
                    queryParameters: query,
                    locationId: LocationResourceIds.SecurityBackingStoreInherit,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: s_apiVersion1,
                    content: null,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public Task<long> RenameTokensAsync(
            Guid securityNamespaceId,
            IEnumerable<TokenRename> renames,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(securityNamespaceId, "securityNamespaceId");
            ArgumentUtility.CheckForNull(renames, "renames");

            using (new OperationScope(LocationResourceIds.SecurityBackingStoreArea, "RenameTokens"))
            {
                return PatchAsync<RenameTokensRequest, long>(
                    value: new RenameTokensRequest(renames),
                    locationId: LocationResourceIds.SecurityBackingStoreTokens,
                    routeValues: new { securityNamespaceId = securityNamespaceId },
                    version: s_apiVersion1,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        private static readonly ApiResourceVersion s_apiVersion1 = new ApiResourceVersion("1.0");
        private static readonly ApiResourceVersion s_apiVersion2 = new ApiResourceVersion("2.0");

        [DataContract]
        private class SetAccessControlListsRequest
        {
            public SetAccessControlListsRequest(
                IEnumerable<AccessControlList> acls,
                IEnumerable<AccessControlEntry> aces,
                bool throwOnInvalidIdentity)
            {
                this.AccessControlLists = acls;
                this.AccessControlEntries = aces;
                this.ThrowOnInvalidIdentity = throwOnInvalidIdentity;
            }

            [DataMember]
            public IEnumerable<AccessControlList> AccessControlLists { get; private set; }

            [DataMember]
            public IEnumerable<AccessControlEntry> AccessControlEntries { get; private set; }

            [DataMember]
            public bool ThrowOnInvalidIdentity { get; private set; }
        }

        [DataContract]
        private class RemoveAccessControlListsRequest
        {
            public RemoveAccessControlListsRequest(
                IEnumerable<String> tokens,
                bool recurse)
            {
                this.Tokens = tokens;
                this.Recurse = recurse;
            }

            [DataMember]
            public IEnumerable<String> Tokens { get; private set; }

            [DataMember]
            public bool Recurse { get; private set; }
        }

        [DataContract]
        private class SetPermissionsRequest
        {
            public SetPermissionsRequest(
                String token,
                IEnumerable<AccessControlEntry> aces,
                bool merge,
                bool throwOnInvalidIdentity)
            {
                this.Token = token;
                this.AccessControlEntries = aces;
                this.Merge = merge;
                this.ThrowOnInvalidIdentity = throwOnInvalidIdentity;
            }

            [DataMember]
            public String Token { get; private set; }

            [DataMember]
            public IEnumerable<AccessControlEntry> AccessControlEntries { get; private set; }

            [DataMember]
            public bool Merge { get; private set; }

            [DataMember]
            public bool ThrowOnInvalidIdentity { get; private set; }
        }

        [DataContract]
        private class RemovePermissionsRequest
        {
            public RemovePermissionsRequest(
                String token,
                IEnumerable<Guid> identityIds)
            {
                this.Token = token;
                this.IdentityIds = identityIds;
            }

            [DataMember]
            public String Token { get; private set; }

            [DataMember]
            public IEnumerable<Guid> IdentityIds { get; private set; }
        }

        [DataContract]
        private class RenameTokensRequest
        {
            public RenameTokensRequest(
                IEnumerable<TokenRename> renames)
            {
                this.Renames = renames;
            }

            [DataMember]
            public IEnumerable<TokenRename> Renames { get; private set; }
        }
    }
}
