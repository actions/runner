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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.WebPlatform;

namespace GitHub.Services.CustomerIntelligence.WebApi
{
    public class CustomerIntelligenceHttpClient : VssHttpClientBase
    {
        public CustomerIntelligenceHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public CustomerIntelligenceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public CustomerIntelligenceHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public CustomerIntelligenceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public CustomerIntelligenceHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="events"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task PublishEventsAsync(
            CustomerIntelligenceEvent[] events,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("b5cc35c2-ff2b-491d-a085-24b6e9f396fd");
            HttpContent content = new ObjectContent<CustomerIntelligenceEvent[]>(events, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
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
