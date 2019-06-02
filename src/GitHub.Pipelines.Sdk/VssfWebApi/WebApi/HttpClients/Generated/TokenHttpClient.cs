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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\genclient.json
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
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi
{
    [ResourceArea(Tokens.TokenResourceIds.AreaId)]
    public class TokenHttpClient : VssHttpClientBase
    {
        public TokenHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TokenHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TokenHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TokenHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TokenHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
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
            Guid locationId = new Guid("94c2bcfb-bf10-4b41-ac01-738122d6b5e0");
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
        /// <param name="key"></param>
        /// <param name="isPublic"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("Use ExchangeAccessTokenKey instead.  This endpoint should be removed after all services are updated to M123.")]
        public Task<AccessToken> GetAccessTokenAsync(
            string key = null,
            bool? isPublic = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("94c2bcfb-bf10-4b41-ac01-738122d6b5e0");
            object routeValues = new { key = key };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (isPublic != null)
            {
                queryParams.Add("isPublic", isPublic.Value.ToString());
            }

            return SendAsync<AccessToken>(
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
        /// <param name="clientId"></param>
        /// <param name="userId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AppSessionTokenResult> IssueAppSessionTokenAsync(
            Guid clientId,
            Guid? userId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("b743b207-6dc5-457b-b1df-b9b63d640f0b");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("clientId", clientId.ToString());
            if (userId != null)
            {
                queryParams.Add("userId", userId.Value.ToString());
            }

            return SendAsync<AppSessionTokenResult>(
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
        /// <param name="appInfo"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AccessTokenResult> ExchangeAppTokenAsync(
            AppTokenSecretPair appInfo,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9ce3c96a-34a2-41af-807d-205da73f227b");
            HttpContent content = new ObjectContent<AppTokenSecretPair>(appInfo, new VssJsonMediaTypeFormatter(true));

            return SendAsync<AccessTokenResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <param name="tokenType"></param>
        /// <param name="isPublic"></param>
        /// <param name="isRequestedByTfsPatWebUI"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<SessionToken> CreateSessionTokenAsync(
            SessionToken sessionToken,
            SessionTokenType? tokenType = null,
            bool? isPublic = null,
            bool? isRequestedByTfsPatWebUI = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");
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
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");
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
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");

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
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");

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
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");
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
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");

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
            Guid locationId = new Guid("ada996bc-8c18-4193-b20c-cd41b13f5b4d");
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
    }
}
