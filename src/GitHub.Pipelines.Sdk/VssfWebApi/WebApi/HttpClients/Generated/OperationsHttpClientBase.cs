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

namespace GitHub.Services.Operations
{
    public abstract class OperationsHttpClientBase : VssHttpClientBase
    {
        public OperationsHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public OperationsHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OperationsHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OperationsHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OperationsHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Gets an operation from the the operationId using the given pluginId.
        /// </summary>
        /// <param name="operationId">The ID for the operation.</param>
        /// <param name="pluginId">The ID for the plugin.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Operation> GetOperationAsync(
            Guid operationId,
            Guid? pluginId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("9a1b74b4-2ca8-4a9f-8470-c2f2e6fdc949");
            object routeValues = new { operationId = operationId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (pluginId != null)
            {
                queryParams.Add("pluginId", pluginId.Value.ToString());
            }

            return SendAsync<Operation>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
