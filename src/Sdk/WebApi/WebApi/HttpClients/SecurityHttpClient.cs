using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Identity;
using GitHub.Services.WebApi;
using System.Threading;

namespace GitHub.Services.Security.Client
{
    [ClientCancellationTimeout(timeoutSeconds: 60)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 10, failurePercentage: 50)]
    public class SecurityHttpClient : VssHttpClientBase
    {

        public SecurityHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public SecurityHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public SecurityHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public SecurityHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public SecurityHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="token"></param>
        /// <param name="requestedPermissions"></param>
        /// <param name="alwaysAllowAdministrators"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<Boolean> HasPermissionAsync(
            Guid securityNamespaceId,
            String token,
            Int32 requestedPermissions,
            Boolean alwaysAllowAdministrators,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(token, "token");

            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/permissions/{0}/{1}", securityNamespaceId, requestedPermissions)));

            List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();
            query.Add(QueryParameters.Token, token);
            query.Add(QueryParameters.AlwaysAllowAdministrators, alwaysAllowAdministrators.ToString());

            uri = uri.AppendQuery(query);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri);

            return SendAsync<Boolean>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="tokens"></param>
        /// <param name="requestedPermissions"></param>
        /// <param name="alwaysAllowAdministrators"></param>
        /// <param name="wireDelimiter">The delimiter to use when encoding the the list of tokens on the wire as a single string</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<Boolean>> HasPermissionsAsync(
            Guid securityNamespaceId,
            IEnumerable<String> tokens,
            Int32 requestedPermissions,
            Boolean alwaysAllowAdministrators,
            char wireDelimiter = ',',
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(tokens, "tokens");

            ApiResourceVersion negotiatedVersion = await NegotiateRequestVersionAsync(
                locationId: LocationResourceIds.SecurityPermissions,
                version: s_pluralHasPermissionVersion,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (null == negotiatedVersion ||
                negotiatedVersion.ResourceVersion < s_pluralHasPermissionVersion.ResourceVersion)
            {
                // Backwards compatibility - plural version of the API not supported by the server

                List<Boolean> toReturn = new List<Boolean>();

                foreach (String token in tokens)
                {
                    bool result = await HasPermissionAsync(
                        securityNamespaceId,
                        token,
                        requestedPermissions,
                        alwaysAllowAdministrators,
                        userState,
                        cancellationToken).ConfigureAwait(false);

                    toReturn.Add(result);
                }

                return toReturn;
            }
            else
            {
                negotiatedVersion = await NegotiateRequestVersionAsync(
                    locationId: LocationResourceIds.SecurityPermissionEvaluationBatch,
                    version: s_batchHasPermissionVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (null != negotiatedVersion)
                {
                    // Batch version of the API supported by the server
                    PermissionEvaluationBatch batch = new PermissionEvaluationBatch() {
                        AlwaysAllowAdministrators = alwaysAllowAdministrators,
                        Evaluations = tokens.Select(token => new PermissionEvaluation() { SecurityNamespaceId = securityNamespaceId, Token = token, Permissions = requestedPermissions }).ToArray()
                    };

                    HttpContent content = new ObjectContent<PermissionEvaluationBatch>(batch, new VssJsonMediaTypeFormatter(true));

                    return await SendAsync<PermissionEvaluationBatch>(
                       new HttpMethod("POST"),
                       locationId: LocationResourceIds.SecurityPermissionEvaluationBatch,
                       version: s_batchHasPermissionVersion,
                       userState: userState,
                       cancellationToken: cancellationToken,
                       content: content).ContinueWith<List<Boolean>>((evalBatch) =>
                       {
                           return evalBatch.Result.Evaluations.Select(e => e.Value).ToList<bool>();
                       }).ConfigureAwait(false);
                }
                else
                {
                    // Plural version of the API supported by the server
                    List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();
                    query.Add(QueryParameters.Tokens, String.Join(wireDelimiter.ToString(), tokens));
                    query.Add(QueryParameters.AlwaysAllowAdministrators, alwaysAllowAdministrators.ToString());
                    query.Add(QueryParameters.Delimiter, wireDelimiter.ToString());

                    return await GetAsync<List<Boolean>>(
                        queryParameters: query,
                        locationId: LocationResourceIds.SecurityPermissions,
                        routeValues: new { securityNamespaceId = securityNamespaceId, permissions = requestedPermissions },
                        version: s_pluralHasPermissionVersion,
                        userState: userState,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static readonly ApiResourceVersion s_pluralHasPermissionVersion = new ApiResourceVersion(new Version(2, 2), 2);
        private static readonly ApiResourceVersion s_batchHasPermissionVersion = new ApiResourceVersion(new Version(3, 0), 1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="token"></param>
        /// <param name="descriptor"></param>
        /// <param name="permissions"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<AccessControlEntry> RemovePermissionAsync(
            Guid securityNamespaceId,
            String token,
            IdentityDescriptor descriptor,
            Int32 permissions,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(token, "token");
            ArgumentUtility.CheckForNull(descriptor, "descriptor");

            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/permissions/{0}/{1}", securityNamespaceId, permissions)));

            List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();
            query.Add(QueryParameters.Token, token);
            query.Add(QueryParameters.Descriptor, descriptor.ToString());

            uri = uri.AppendQuery(query);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, uri.AbsoluteUri);

            return SendAsync<AccessControlEntry>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="token"></param>
        /// <param name="accessControlEntries"></param>
        /// <param name="merge"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<IEnumerable<AccessControlEntry>> SetAccessControlEntriesAsync(
            Guid securityNamespaceId,
            String token,
            IEnumerable<AccessControlEntry> accessControlEntries,
            Boolean merge,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(token, "token");
            ArgumentUtility.CheckForNull(accessControlEntries, "accessControlEntries");

            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/accesscontrolentries/{0}", securityNamespaceId)));

            SetAccessControlEntriesInfo info = new SetAccessControlEntriesInfo(token, accessControlEntries, merge);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri.AbsoluteUri);
            message.Content = new ObjectContent<SetAccessControlEntriesInfo>(info, base.Formatter);

            return SendAsync<IEnumerable<AccessControlEntry>>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="token"></param>
        /// <param name="descriptors"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<Boolean> RemoveAccessControlEntriesAsync(
            Guid securityNamespaceId,
            String token,
            IEnumerable<IdentityDescriptor> descriptors,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(token, "token");

            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/accesscontrolentries/{0}", securityNamespaceId)));

            List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();
            query.Add(QueryParameters.Token, token);

            if (descriptors != null)
            {
                query.AddMultiple(QueryParameters.Descriptors, descriptors, (descriptor) => { return descriptor.ToString(); });
            }

            uri = uri.AppendQuery(query);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, uri.AbsoluteUri);

            return SendAsync<Boolean>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="token"></param>
        /// <param name="descriptors"></param>
        /// <param name="includeExtendedInfo"></param>
        /// <param name="recurse"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<IEnumerable<AccessControlList>> QueryAccessControlListsAsync(
            Guid securityNamespaceId,
            String token,
            IEnumerable<IdentityDescriptor> descriptors,
            Boolean includeExtendedInfo,
            Boolean recurse,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/accesscontrollists/{0}", securityNamespaceId)));

            List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();
            if (token != null)
            {
                query.Add(QueryParameters.Token, token);
            }
            query.Add(QueryParameters.IncludeExtendedInfo, includeExtendedInfo.ToString());
            query.Add(QueryParameters.Recurse, recurse.ToString());

            if (descriptors != null)
            {
                query.AddMultiple(QueryParameters.Descriptors, descriptors, (descriptor) => { return descriptor.ToString(); });
            }

            uri = uri.AppendQuery(query);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri);

            return SendAsync<IEnumerable<AccessControlList>>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="accessControlLists"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> SetAccessControlListsAsync(
            Guid securityNamespaceId,
            IEnumerable<AccessControlList> accessControlLists,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(accessControlLists, "accessControlLists");

            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/accesscontrollists/{0}", securityNamespaceId)));

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri.AbsoluteUri);
            message.Content = new ObjectContent<VssJsonCollectionWrapper<AccessControlListsCollection>>(
                new VssJsonCollectionWrapper<AccessControlListsCollection>(new AccessControlListsCollection(accessControlLists)), base.Formatter);

            return SendAsync(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="tokens"></param>
        /// <param name="recurse"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<Boolean> RemoveAccessControlListsAsync(
            Guid securityNamespaceId,
            IEnumerable<String> tokens,
            Boolean recurse,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/accesscontrollists/{0}", securityNamespaceId)));

            List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();

            if (tokens != null)
            {
                query.AddMultiple(QueryParameters.Tokens, tokens);
            }

            query.Add(QueryParameters.Recurse, recurse.ToString());

            uri = uri.AppendQuery(query);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, uri.AbsoluteUri);

            return SendAsync<Boolean>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="userState"></param>
        /// <returns></returns>
        public Task<IEnumerable<SecurityNamespaceDescription>> QuerySecurityNamespacesAsync(
            Guid securityNamespaceId,
            bool localOnly = false,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/securitynamespaces/{0}", securityNamespaceId)));

            List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();

            query.Add(QueryParameters.LocalOnly, localOnly.ToString());

            uri = uri.AppendQuery(query);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri);

            return SendAsync<IEnumerable<SecurityNamespaceDescription>>(message, userState, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="securityNamespaceId"></param>
        /// <param name="token"></param>
        /// <param name="inherit"></param>
        /// <param name="userState"></param>
        public Task<HttpResponseMessage> SetInheritFlagAsync(
            Guid securityNamespaceId,
            String token,
            Boolean inherit,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(token, "token");

            Uri uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), String.Format(CultureInfo.InvariantCulture, "/_apis/securitynamespaces/{0}", securityNamespaceId)));

            SetInheritFlagInfo info = new SetInheritFlagInfo(token, inherit);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri.AbsoluteUri);
            message.Content = new ObjectContent<SetInheritFlagInfo>(info, base.Formatter);

            return SendAsync(message, userState, cancellationToken);
        }
    }


}
