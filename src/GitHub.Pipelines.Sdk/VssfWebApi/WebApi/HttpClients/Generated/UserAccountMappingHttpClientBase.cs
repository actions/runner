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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\useraccountmapping.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.UserAccountMapping.Client
{
    [ResourceArea(UserAccountMappingResourceIds.AreaId)]
    public abstract class UserAccountMappingHttpClientBase : VssHttpClientBase
    {
        public UserAccountMappingHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public UserAccountMappingHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public UserAccountMappingHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public UserAccountMappingHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public UserAccountMappingHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="accountId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task ActivateUserAccountMappingAsync(
            string descriptor,
            Guid accountId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("ce4b4a54-f365-4fcc-b623-4a3f8e7bc07c");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("accountId", accountId.ToString());

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
        /// <param name="descriptor"></param>
        /// <param name="accountId"></param>
        /// <param name="userRole"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task ActivateUserAccountMappingAsync(
            string descriptor,
            Guid accountId,
            UserRole userRole,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("ce4b4a54-f365-4fcc-b623-4a3f8e7bc07c");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("accountId", accountId.ToString());
            queryParams.Add("userRole", userRole.ToString());

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
        /// <param name="descriptor"></param>
        /// <param name="accountId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task DeactivateUserAccountMappingAsync(
            string descriptor,
            Guid accountId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("ce4b4a54-f365-4fcc-b623-4a3f8e7bc07c");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("accountId", accountId.ToString());

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
        /// <param name="descriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<bool> HasMappingsAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ce4b4a54-f365-4fcc-b623-4a3f8e7bc07c");
            object routeValues = new { descriptor = descriptor };

            return SendAsync<bool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="userRole"></param>
        /// <param name="useEqualsCheckForUserRoleMatch"></param>
        /// <param name="includeDeletedAccounts"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<Guid>> QueryAccountIdsAsync(
            string descriptor,
            UserRole userRole,
            bool? useEqualsCheckForUserRoleMatch = null,
            bool? includeDeletedAccounts = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ce4b4a54-f365-4fcc-b623-4a3f8e7bc07c");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userRole", userRole.ToString());
            if (useEqualsCheckForUserRoleMatch != null)
            {
                queryParams.Add("useEqualsCheckForUserRoleMatch", useEqualsCheckForUserRoleMatch.Value.ToString());
            }
            if (includeDeletedAccounts != null)
            {
                queryParams.Add("includeDeletedAccounts", includeDeletedAccounts.Value.ToString());
            }

            return SendAsync<List<Guid>>(
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
        /// <param name="descriptor"></param>
        /// <param name="accountId"></param>
        /// <param name="maxVsLevelFromAccountLicense"></param>
        /// <param name="maxVsLevelFromAccountExtensions"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task SetUserAccountLicenseInfoAsync(
            string descriptor,
            Guid accountId,
            VisualStudioLevel maxVsLevelFromAccountLicense,
            VisualStudioLevel maxVsLevelFromAccountExtensions,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("ce4b4a54-f365-4fcc-b623-4a3f8e7bc07c");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("accountId", accountId.ToString());
            queryParams.Add("maxVsLevelFromAccountLicense", maxVsLevelFromAccountLicense.ToString());
            queryParams.Add("maxVsLevelFromAccountExtensions", maxVsLevelFromAccountExtensions.ToString());

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
