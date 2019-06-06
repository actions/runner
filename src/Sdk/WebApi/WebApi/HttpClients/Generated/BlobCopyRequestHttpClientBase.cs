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
*   https://vsowiki.com/index.php?title=Rest_Client_Generation
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

namespace GitHub.Services.Zeus
{
    [ResourceArea(BlobCopyLocationIds.ResourceString)]
    public abstract class BlobCopyRequestHttpClientBase : VssHttpClientBase
    {
        public BlobCopyRequestHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public BlobCopyRequestHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public BlobCopyRequestHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public BlobCopyRequestHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public BlobCopyRequestHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        public virtual Task<HttpResponseMessage> DeleteBlobCopyRequestAsync(
            int requestId,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("8907fe1c-346a-455b-9ab9-dde883687231");
            Object routeValues = new { requestId = requestId };

            return SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        public virtual Task<BlobCopyRequest> GetBlobCopyRequestAsync(
            int requestId,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8907fe1c-346a-455b-9ab9-dde883687231");
            Object routeValues = new { requestId = requestId };

            return SendAsync<BlobCopyRequest>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual Task<List<BlobCopyRequest>> GetBlobCopyRequestsAsync(
        
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8907fe1c-346a-455b-9ab9-dde883687231");

            return SendAsync<List<BlobCopyRequest>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        public virtual Task<BlobCopyRequest> QueueBlobCopyRequestAsync(
            BlobCopyRequest request,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("8907fe1c-346a-455b-9ab9-dde883687231");
            HttpContent content = new ObjectContent<BlobCopyRequest>(request, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BlobCopyRequest>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        public virtual Task<BlobCopyRequest> UpdateBlobCopyRequestAsync(
            BlobCopyRequest request,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("8907fe1c-346a-455b-9ab9-dde883687231");
            HttpContent content = new ObjectContent<BlobCopyRequest>(request, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BlobCopyRequest>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("2.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content
            );
        }
    }
}
