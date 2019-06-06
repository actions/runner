/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 *
 * ---------------------------------------------------------
 * Generated file, DO NOT EDIT
 * ---------------------------------------------------------
 *
 * See following wiki page for instructions on how to regenerate:
 *   https://aka.ms/azure-devops-client-generation
 *
 * Configuration file:
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\tokenissue.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.DelegatedAuthorization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Tokens.WebApi
{
    [ResourceArea(TokenIssueResourceIds.AreaId)]
    public class TokenIssueHttpClient : VssHttpClientBase
    {
        public TokenIssueHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TokenIssueHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TokenIssueHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TokenIssueHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TokenIssueHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="accessTokenKey"></param>
        /// <param name="isPublic"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AccessToken> ExchangeAccessTokenKeyAsync(
            string accessTokenKey,
            bool? isPublic = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("24691e90-c8bd-42c0-8aae-71b7511a797a");
            HttpContent content = new ObjectContent<string>(accessTokenKey, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }

            return SendAsync<AccessToken>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subjectDescriptor"></param>
        /// <param name="clientId"></param>
        /// <param name="authorizationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AppSessionTokenResult> IssueAppSessionTokenAsync(
            SubjectDescriptor subjectDescriptor,
            Guid clientId,
            Guid? authorizationId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("325f73ea-e978-4ad1-8f3a-c30b39000a17");
            object routeValues = new { subjectDescriptor = subjectDescriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("clientId", clientId.ToString());
            if (authorizationId != null)
            {
                queryParams.Add("authorizationId", authorizationId.Value.ToString());
            }

            return SendAsync<AppSessionTokenResult>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="appInfo"></param>
        /// <param name="accessId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AccessTokenResult> ExchangeAppTokenAsync(
            AppTokenSecretPair appInfo,
            Guid? accessId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9030cb81-c1fd-4f3b-9910-c90eb559b830");
            HttpContent content = new ObjectContent<AppTokenSecretPair>(appInfo, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (accessId != null)
            {
                queryParams.Add("accessId", accessId.Value.ToString());
            }

            return SendAsync<AccessTokenResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <param name="hostId"></param>
        /// <param name="orgHostId"></param>
        /// <param name="deploymentHostId"></param>
        /// <param name="tokenType"></param>
        /// <param name="isPublic"></param>
        /// <param name="isRequestedByTfsPatWebUI"></param>
        /// <param name="isImpersonating"></param>
        /// <param name="secretToken"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<SessionToken> CreateSessionTokenAsync(
            SessionToken sessionToken,
            Guid hostId,
            Guid orgHostId,
            Guid deploymentHostId,
            SessionTokenType? tokenType = null,
            bool? isPublic = null,
            bool? isRequestedByTfsPatWebUI = null,
            bool? isImpersonating = null,
            string secretToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");
            HttpContent content = new ObjectContent<SessionToken>(sessionToken, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("hostId", hostId.ToString());
            queryParams.Add("orgHostId", orgHostId.ToString());
            queryParams.Add("deploymentHostId", deploymentHostId.ToString());
            if (tokenType != null)
            {
                queryParams.Add("tokenType", tokenType.Value.ToString());
            }
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }
            if (isRequestedByTfsPatWebUI != null)
            {
                queryParams.Add("isRequestedByTfsPatWebUI", isRequestedByTfsPatWebUI.Value.ToString());
            }
            if (isImpersonating != null)
            {
                queryParams.Add("isImpersonating", isImpersonating.Value.ToString());
            }
            if (secretToken != null)
            {
                queryParams.Add("secretToken", secretToken);
            }

            return SendAsync<SessionToken>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="authorizationId"></param>
        /// <param name="isPublic"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<SessionToken> GetSessionTokenAsync(
            Guid authorizationId,
            bool? isPublic = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");
            object routeValues = new { authorizationId = authorizationId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }

            return SendAsync<SessionToken>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="isPublic"></param>
        /// <param name="includePublicData"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<SessionToken>> GetSessionTokensAsync(
            bool? isPublic = null,
            bool? includePublicData = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }
            if (includePublicData != null)
            {
                queryParams.Add("includePublicData", includePublicData.Value.ToString());
            }

            return SendAsync<List<SessionToken>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="displayFilterOption"></param>
        /// <param name="createdByOption"></param>
        /// <param name="sortByOption"></param>
        /// <param name="isSortAscending"></param>
        /// <param name="startRowNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageRequestTimeStamp"></param>
        /// <param name="isPublic"></param>
        /// <param name="includePublicData"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<PagedSessionTokens> GetSessionTokensPageAsync(
            DisplayFilterOptions displayFilterOption,
            CreatedByOptions createdByOption,
            SortByOptions sortByOption,
            bool isSortAscending,
            int startRowNumber,
            int pageSize,
            string pageRequestTimeStamp,
            bool? isPublic = null,
            bool? includePublicData = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("displayFilterOption", displayFilterOption.ToString());
            queryParams.Add("createdByOption", createdByOption.ToString());
            queryParams.Add("sortByOption", sortByOption.ToString());
            queryParams.Add("isSortAscending", isSortAscending.ToString());
            queryParams.Add("startRowNumber", startRowNumber.ToString(CultureInfo.InvariantCulture));
            queryParams.Add("pageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            queryParams.Add("pageRequestTimeStamp", pageRequestTimeStamp);
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }
            if (includePublicData != null)
            {
                queryParams.Add("includePublicData", includePublicData.Value.ToString());
            }

            return SendAsync<PagedSessionTokens>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="publicData"></param>
        /// <param name="remove"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RemovePublicKeyAsync(
            SshPublicKey publicData,
            bool remove,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");
            HttpContent content = new ObjectContent<SshPublicKey>(publicData, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("remove", remove.ToString());

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RevokeAllSessionTokensOfUserAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="authorizationId"></param>
        /// <param name="isPublic"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RevokeSessionTokenAsync(
            Guid authorizationId,
            bool? isPublic = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");
            object routeValues = new { authorizationId = authorizationId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="authorizationId"></param>
        /// <param name="sessionToken"></param>
        /// <param name="tokenType"></param>
        /// <param name="isPublic"></param>
        /// <param name="isRequestedByTfsPatWebUI"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<SessionToken> UpdateSessionTokenAsync(
            Guid authorizationId,
            SessionToken sessionToken,
            SessionTokenType? tokenType = null,
            bool? isPublic = null,
            bool? isRequestedByTfsPatWebUI = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("98e25729-952a-4b1f-ac89-7ca8b9803261");
            object routeValues = new { authorizationId = authorizationId };
            HttpContent content = new ObjectContent<SessionToken>(sessionToken, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (tokenType != null)
            {
                queryParams.Add("tokenType", tokenType.Value.ToString());
            }
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }
            if (isRequestedByTfsPatWebUI != null)
            {
                queryParams.Add("isRequestedByTfsPatWebUI", isRequestedByTfsPatWebUI.Value.ToString());
            }

            return SendAsync<SessionToken>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }
    }
}
