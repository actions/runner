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
 *   sps\clients\csm.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Commerce
{
    [ResourceArea(CsmResourceIds.AreaId)]
    public abstract class CsmHttpClientBase : VssHttpClientBase
    {
        public CsmHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public CsmHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public CsmHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public CsmHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public CsmHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="resourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<AccountResource> Accounts_CreateOrUpdateAsync(
            AccountResourceRequest requestData,
            Guid subscriptionId,
            string resourceGroupName,
            string resourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("5745408e-6e9e-49c7-92bf-62932c8df69d");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, resourceName = resourceName };
            HttpContent content = new ObjectContent<AccountResourceRequest>(requestData, new VssJsonMediaTypeFormatter(true));

            return SendAsync<AccountResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="resourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task Accounts_DeleteAsync(
            Guid subscriptionId,
            string resourceGroupName,
            string resourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("5745408e-6e9e-49c7-92bf-62932c8df69d");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, resourceName = resourceName };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
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
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="resourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<AccountResource> Accounts_GetAsync(
            Guid subscriptionId,
            string resourceGroupName,
            string resourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("5745408e-6e9e-49c7-92bf-62932c8df69d");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, resourceName = resourceName };

            return SendAsync<AccountResource>(
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
        /// <param name="requestData"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="resourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<AccountResource> Accounts_UpdateAsync(
            AccountTagRequest requestData,
            Guid subscriptionId,
            string resourceGroupName,
            string resourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("5745408e-6e9e-49c7-92bf-62932c8df69d");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, resourceName = resourceName };
            HttpContent content = new ObjectContent<AccountTagRequest>(requestData, new VssJsonMediaTypeFormatter(true));

            return SendAsync<AccountResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<AccountResourceListResult> Accounts_ListByResourceGroupAsync(
            Guid subscriptionId,
            string resourceGroupName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("73d8b171-a2a0-4ac6-ba0b-ef762098e5ec");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName };

            return SendAsync<AccountResourceListResult>(
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<OperationListResult> Operations_ListAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("454d976b-812e-4947-bc4e-c2c23160317e");

            return SendAsync<OperationListResult>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountResourceName"></param>
        /// <param name="extensionResourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<ExtensionResource> Extensions_CreateAsync(
            ExtensionResourceRequest requestData,
            Guid subscriptionId,
            string resourceGroupName,
            string accountResourceName,
            string extensionResourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("9cb405cb-4a72-4a50-ab6d-be1da1726c33");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, accountResourceName = accountResourceName, extensionResourceName = extensionResourceName };
            HttpContent content = new ObjectContent<ExtensionResourceRequest>(requestData, new VssJsonMediaTypeFormatter(true));

            return SendAsync<ExtensionResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountResourceName"></param>
        /// <param name="extensionResourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task Extensions_DeleteAsync(
            Guid subscriptionId,
            string resourceGroupName,
            string accountResourceName,
            string extensionResourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("9cb405cb-4a72-4a50-ab6d-be1da1726c33");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, accountResourceName = accountResourceName, extensionResourceName = extensionResourceName };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
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
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountResourceName"></param>
        /// <param name="extensionResourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<ExtensionResource> Extensions_GetAsync(
            Guid subscriptionId,
            string resourceGroupName,
            string accountResourceName,
            string extensionResourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("9cb405cb-4a72-4a50-ab6d-be1da1726c33");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, accountResourceName = accountResourceName, extensionResourceName = extensionResourceName };

            return SendAsync<ExtensionResource>(
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
        /// <param name="requestData"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountResourceName"></param>
        /// <param name="extensionResourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<ExtensionResource> Extensions_UpdateAsync(
            ExtensionResourceRequest requestData,
            Guid subscriptionId,
            string resourceGroupName,
            string accountResourceName,
            string extensionResourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("9cb405cb-4a72-4a50-ab6d-be1da1726c33");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, accountResourceName = accountResourceName, extensionResourceName = extensionResourceName };
            HttpContent content = new ObjectContent<ExtensionResourceRequest>(requestData, new VssJsonMediaTypeFormatter(true));

            return SendAsync<ExtensionResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="accountResourceName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<ExtensionResourceListResult> Extensions_ListByAccountAsync(
            Guid subscriptionId,
            string resourceGroupName,
            string accountResourceName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a509d9a8-d23f-4e0f-a69f-ad52b248943b");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, accountResourceName = accountResourceName };

            return SendAsync<ExtensionResourceListResult>(
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
        /// <param name="request"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<CheckNameAvailabilityResult> Accounts_CheckNameAvailabilityAsync(
            CheckNameAvailabilityParameter request,
            Guid subscriptionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("031d6b9b-a0d4-4b46-97c5-9ddaca1aa5cd");
            object routeValues = new { subscriptionId = subscriptionId };
            HttpContent content = new ObjectContent<CheckNameAvailabilityParameter>(request, new VssJsonMediaTypeFormatter(true));

            return SendAsync<CheckNameAvailabilityResult>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="resourcesMoveRequest"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="operationName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task MoveResourcesAsync(
            ResourcesMoveRequest resourcesMoveRequest,
            Guid subscriptionId,
            string resourceGroupName,
            string operationName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("9e0fa51b-9d61-4899-a5a1-e1f0f5e75bc0");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, operationName = operationName };
            HttpContent content = new ObjectContent<ResourcesMoveRequest>(resourcesMoveRequest, new VssJsonMediaTypeFormatter(true));

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
        /// <param name="requestData"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual async Task HandleNotificationAsync(
            CsmSubscriptionRequest requestData,
            Guid subscriptionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("97bc4c4d-ce2e-4ca3-87cc-2bd07aeee500");
            object routeValues = new { subscriptionId = subscriptionId };
            HttpContent content = new ObjectContent<CsmSubscriptionRequest>(requestData, new VssJsonMediaTypeFormatter(true));

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
        /// <param name="subscriptionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<CsmSubscriptionResourceListResult> SubscriptionResources_ListAsync(
            Guid subscriptionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f34be62f-f215-4bda-8b57-9e8a7a5fd66a");
            object routeValues = new { subscriptionId = subscriptionId };

            return SendAsync<CsmSubscriptionResourceListResult>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
