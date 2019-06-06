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
 *   sps\clients\identity\clientgeneratorconfigs\genclient.json
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

namespace GitHub.Services.Identity.Mru.Client
{
    [ResourceArea(IdentityMruResourceIds.AreaId)]
    public abstract class IdentityMruHttpClientBase : VssHttpClientBase
    {
        public IdentityMruHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public IdentityMruHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public IdentityMruHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public IdentityMruHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public IdentityMruHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="identityId"></param>
        /// <param name="containerId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Guid>> GetMruIdentitiesAsync(
            string identityId,
            Guid containerId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("15d952a1-bb4e-436c-88ca-cfe1e9ff3331");
            object routeValues = new { identityId = identityId, containerId = containerId };

            return SendAsync<List<Guid>>(
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
        /// <param name="identityIds"></param>
        /// <param name="identityId"></param>
        /// <param name="containerId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task SetMruIdentitiesAsync(
            IEnumerable<Guid> identityIds,
            string identityId,
            Guid containerId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("15d952a1-bb4e-436c-88ca-cfe1e9ff3331");
            object routeValues = new { identityId = identityId, containerId = containerId };
            HttpContent content = new ObjectContent<IEnumerable<Guid>>(identityIds, new VssJsonMediaTypeFormatter(true));

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
        /// <param name="updateData"></param>
        /// <param name="identityId"></param>
        /// <param name="containerId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task UpdateMruIdentitiesAsync(
            MruIdentitiesUpdateData updateData,
            string identityId,
            Guid containerId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("15d952a1-bb4e-436c-88ca-cfe1e9ff3331");
            object routeValues = new { identityId = identityId, containerId = containerId };
            HttpContent content = new ObjectContent<MruIdentitiesUpdateData>(updateData, new VssJsonMediaTypeFormatter(true));

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
    }
}
