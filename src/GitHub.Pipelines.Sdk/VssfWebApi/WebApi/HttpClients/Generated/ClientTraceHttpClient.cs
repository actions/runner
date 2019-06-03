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

namespace GitHub.Services.ClientTrace.WebApi
{
    public class ClientTraceHttpClient : VssHttpClientBase
    {
        public ClientTraceHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public ClientTraceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ClientTraceHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ClientTraceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ClientTraceHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
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
            ClientTraceEvent[] events,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("06bcc74a-1491-4eb8-a0eb-704778f9d041");
            HttpContent content = new ObjectContent<ClientTraceEvent[]>(events, new VssJsonMediaTypeFormatter(true));

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
