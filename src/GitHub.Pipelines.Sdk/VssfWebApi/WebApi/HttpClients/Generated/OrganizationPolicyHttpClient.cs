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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\organizationpolicy.genclient.json
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
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.Organization.Client
{
    public class OrganizationPolicyHttpClient : VssHttpClientBase
    {
        public OrganizationPolicyHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public OrganizationPolicyHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OrganizationPolicyHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OrganizationPolicyHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OrganizationPolicyHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="policyName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Policy> GetPolicyAsync(
            string policyName,
            string defaultValue,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d0ab077b-1b97-4f78-984c-cfe2d248fc79");
            object routeValues = new { policyName = policyName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("defaultValue", defaultValue);

            return SendAsync<Policy>(
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
        /// <param name="patchDocument"></param>
        /// <param name="policyName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task UpdatePolicyAsync(
            JsonPatchDocument patchDocument,
            string policyName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("d0ab077b-1b97-4f78-984c-cfe2d248fc79");
            object routeValues = new { policyName = policyName };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
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
        /// <param name="policyNames"></param>
        /// <param name="defaultValues"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Dictionary<string, Policy>> GetPoliciesAsync(
            IEnumerable<string> policyNames,
            IEnumerable<string> defaultValues,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7ef423e0-59d8-4c00-b951-7143b18bd97b");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            string policyNamesAsString = null;
            if (policyNames != null)
            {
                policyNamesAsString = string.Join(",", policyNames);
            }
            queryParams.Add("policyNames", policyNamesAsString);
            string defaultValuesAsString = null;
            if (defaultValues != null)
            {
                defaultValuesAsString = string.Join(",", defaultValues);
            }
            queryParams.Add("defaultValues", defaultValuesAsString);

            return SendAsync<Dictionary<string, Policy>>(
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
        /// <param name="policyName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PolicyInfo> GetPolicyInformationAsync(
            string policyName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("222af71b-7280-4a95-80e4-dcb0deeac834");
            object routeValues = new { policyName = policyName };

            return SendAsync<PolicyInfo>(
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
        /// <param name="policyNames"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Dictionary<string, PolicyInfo>> GetPolicyInformationsAsync(
            IEnumerable<string> policyNames = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("222af71b-7280-4a95-80e4-dcb0deeac834");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (policyNames != null && policyNames.Any())
            {
                queryParams.Add("policyNames", string.Join(",", policyNames));
            }

            return SendAsync<Dictionary<string, PolicyInfo>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
