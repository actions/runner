using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GitHub.Services.Content.Common.Tracing
{
    public static class HttpRequestExceptionExtensions
    {
        private static readonly HashSet<string> responseHeaderNamesToCapture = new HashSet<string>(new string[] {

            // AFD
            "X-CID", // For AFD, always X-CID: 7
            "X-CCC", // e.g. X-CCC: US, the country code of the country where the Edge node is located
            GitHub.Services.Common.Internal.HttpHeaders.AfdResponseRef, // e.g. X-MSEdge-Ref: Ref A: E742FFB921A84945820574C587A70DE4 Ref B: CO1EDGE0418 Ref C: 2018-04-04T19:42:27Z
            "X-MS-Ref-OriginShield", // e.g. X-MS-Ref-OriginShield: Ref A: 86DEBAD3CFC941AD80A2F3149156C9EF Ref B: BAYEDGE0522 Ref C: 2018-04-04T19:31:51Z

            // Azure Blob headers per https://docs.microsoft.com/en-us/rest/api/storageservices/get-blob-properties
            //"ETag", // e.g. ETag: "0x8D58320C5494D7E"
            // "x-ms-blob-type", // For AS, x-ms-blob-type is always BlockBlob
            // x-ms-request-id: "uniquely identifies the request that was made and can be used for troubleshooting the request"
            // https://docs.microsoft.com/en-us/rest/api/storageservices/troubleshooting-api-operations
            // e.g. x-ms-request-id: 8c7c2edf-d01e-0044-024b-cc0fda000000
            "x-ms-request-id",

            // // e.g. x-ms-version: 2017-04-17
            // https://docs.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services
            "x-ms-version",

            // Azure Blob SAS URIs
            "x-ms-lease-state", // x-ms-lease-state: available
            "x-ms-lease-status", // x-ms-lease-status: unlocked

            // Azure Blob Soft Delete state?
            // https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-soft-delete
            "x-ms-meta-keepUntil", // e.g. x-ms-meta-keepUntil: 131650455780000000

            // Azure Blob Storage Tiers
            // https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-storage-tiers
            "x-ms-access-tier",
            "x-ms-access-tier-inferred",
            "x-ms-archive-status",
            "x-ms-access-tier-change-time"

            }, StringComparer.OrdinalIgnoreCase);

        private const string exceptionDataResponseKey = "LastRequestResponse";


        public static bool SetHttpMessagesForTracing(this HttpRequestException exception, HttpRequestMessage request, HttpResponseMessage response)
        {
            try
            {
                var responseHeaderDetails = SerializeRequestResponse(request, response);
                exception.Data[exceptionDataResponseKey] = responseHeaderDetails;
            }
            catch (Exception)
            {
#if DEBUG
                // Diagnostics should never fail the caller unless debugging
                throw;
#endif
            }

            // We return false so that if called from an exception filter, we avoid entering the catch block which would unwind the stack.
            // For example
            // try
            // {
            //     response = await httpClient.SendAsync(request, ...
            // ...
            // catch (HttpRequestException e) when (e.SetResponse(response))
            // ...
            return false;
        }

        /// <remarks>
        /// Will return HTTP details on exceptions that have it supplied, otherwise the exception message is returned.
        /// </remarks>
        public static string GetHttpMessageDetailsForTracing(this Exception exception)
        {
            string details = null;

            if (exception.Data.Contains(exceptionDataResponseKey))
            {
                object data = exception.Data[exceptionDataResponseKey];
                if (data != null)
                {
                    var text = data as string;
                    if (text != null)
                    {
                        details = text;
                    }
                }
            }

            if (details == null)
            {
                details = $"No {exceptionDataResponseKey} on exception {exception.GetType().Name}: {exception.Message}";
            }

            return details;
        }

        private static string SerializeRequestResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            var details = new StringBuilder();

            if (request == null && response != null)
            {
                request = response.RequestMessage;
            }

            if (request != null)
            {
                // Request properties

                // Strip query string which contains the SAS signature
                var tracedUri = request.RequestUri.GetLeftPart(UriPartial.Path);

                details.AppendLine($"{nameof(HttpRequestMessage)}.{nameof(request.Method)}: {request.Method.ToString()}");
                details.AppendLine($"{nameof(HttpRequestMessage)}.{nameof(request.RequestUri)}: {tracedUri}");
            }

            if (response != null)
            {
                // Response properties

                details.AppendLine($"{nameof(HttpResponseMessage)}.{nameof(response.StatusCode)}: {(int)response.StatusCode} {response.StatusCode.ToString()}");

                if (response?.Content?.Headers != null)
                {
                    try
                    {
                        var contentHeaders = response.Content.Headers;

                        if (contentHeaders.ContentLength.HasValue)
                        {
                            // e.g. Content-Length: 18657
                            details.AppendLine($"{nameof(HttpResponseMessage)}.{nameof(response.Content.Headers.ContentLength)}: {response.Content.Headers.ContentLength}");
                        }

                        if (contentHeaders.LastModified.HasValue)
                        {
                            // Date uploaded to Azure Blob e.g. Last-Modified: Tue, 06 Mar 2018 05:11:51 GMT
                            var lastModified = string.Format(CultureInfo.InvariantCulture, "{0}", contentHeaders.LastModified);
                            details.AppendLine($"{nameof(HttpResponseMessage)}.{nameof(contentHeaders.LastModified)}: {lastModified}");
                        }
                    }
                    catch (ObjectDisposedException)
                    {
#if DEBUG
                        // If HttpResponseMessage.EnsureSuccessStatusCode was called before us,
                        // for example, we're called by an exception filter of it,
                        // then the HttpContent would be disposed by now, and we don't want to fail
                        // the caller on telemetry collection unless debugging.
                        throw;
#endif
                    }
                }

                if (response.Headers.ETag != null)
                {
                    // e.g. ETag: "0x8D58320C5494D7E"
                    details.AppendLine($"{nameof(HttpResponseMessage)}.{nameof(response.Headers.ETag)}: {response.Headers.ETag.Tag}");
                }

                if (response.Headers.Server != null)
                {
                    // e.g. Server: Windows-Azure-Blob/1.0 Microsoft-HTTPAPI/2.0
                    var serverValue = string.Join(" ", response.Headers.Server.Select(s =>
                        s.Product.ToString() + (string.IsNullOrEmpty(s.Comment) ? string.Empty : " " + s.Comment)));
                    details.AppendLine($"{nameof(HttpResponseMessage)}.{nameof(response.Headers.Server)}: {serverValue}");
                }

                foreach (var headerName in responseHeaderNamesToCapture)
                {
                    // Note that HttpResponseHeaders is backed by a non-case-sensitive dictionary so header lookups with varying case will succeed.
                    // Source reference:
                    // static HttpResponseHeaders()
                    // {
                    //     parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        if (response.Headers.Contains(headerName))
                        {
                            var values = string.Join(",", response.Headers.GetValues(headerName).ToArray());
                            details.AppendLine($"{nameof(HttpResponseHeaders)}.{headerName}: {values}");
                        }
                    }
                    catch (Exception e) when (e.StackTrace.Contains("CheckHeaderName"))
                    {
#if DEBUG
                        // Ignore exceptions from HttpHeaders.CheckHeaderName unless debugging
                        throw;
#endif
                    }
                }
            }

            return details.ToString();
        }
    }
}
