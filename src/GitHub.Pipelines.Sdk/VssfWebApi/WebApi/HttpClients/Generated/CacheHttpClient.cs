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
 *   sps\clients\identity\clientgeneratorconfigs\propertycache.genclient.json
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

namespace GitHub.Services.Identity.Client
{
    [ResourceArea(PropertyCacheResourceIds.AreaId)]
    public class CacheHttpClient : VssHttpClientBase
    {
        public CacheHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public CacheHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public CacheHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public CacheHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public CacheHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<string> CacheAsync(
            Object data,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("656342eb-ae7d-4ff2-802f-19c6e35b0fe6");
            HttpContent content = new ObjectContent<Object>(data, new VssJsonMediaTypeFormatter(true));

            return SendAsync<string>(
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
        /// <param name="cacheKey"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task DeleteAsync(
            string cacheKey,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("656342eb-ae7d-4ff2-802f-19c6e35b0fe6");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("cacheKey", cacheKey);

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
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
        /// <param name="cacheKey"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Object> GetAsync(
            string cacheKey,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("656342eb-ae7d-4ff2-802f-19c6e35b0fe6");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("cacheKey", cacheKey);

            return SendAsync<Object>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
