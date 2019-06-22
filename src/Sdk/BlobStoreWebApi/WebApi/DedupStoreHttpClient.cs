using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Interfaces.Utils;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.BlobStore.WebApi
{
    [ResourceArea(areaId: ResourceIds.DedupAreaId)]
    public class DedupStoreHttpClient : ArtifactHttpClient, IDedupStoreHttpClient
    {
        private const string EnvironmentVariablePrefix = "VSO_DEDUP_";

        public static readonly StringWithQualityHeaderValue XpressCompressionHeader =
            new StringWithQualityHeaderValue(DedupConstants.XpressCompressionHeaderString);

        /* https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.110).aspx#Anchor_5
           The following methods are thread safe:
            CancelPendingRequests
            DeleteAsync
            GetAsync
            GetByteArrayAsync
            GetStreamAsync
            GetStringAsync
            PostAsync
            PutAsync
            SendAsync
        */
        private static readonly HttpClient basicClient;

        private HttpClient proxyHttpClient;

        static DedupStoreHttpClient()
        {
            string proxy = Environment.GetEnvironmentVariable($"{EnvironmentVariablePrefix}PROXY");
            if (string.IsNullOrWhiteSpace(proxy))
            {
                basicClient = new HttpClient();
            }
            else
            {
                basicClient = new HttpClient(new HttpClientHandler
                {
                    Proxy = new WebProxy(proxy, BypassOnLocal: false),
                    UseProxy = true
                });
            }

            if ("1" == Environment.GetEnvironmentVariable($"{EnvironmentVariablePrefix}DISABLE_TIMEOUT"))
            {
                // Work-around global Timer lock contention
                // http://blog.i3arnon.com/2015/07/25/surprising-timer-contention/
                basicClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
            }
        }

        private long calls;
        private long throttledCalls;
        private long xCacheHits;
        private long xCacheMisses;

        public long Calls => calls;
        public long ThrottledCalls => throttledCalls;
        public long XCacheHits => xCacheHits;
        public long XCacheMisses => xCacheMisses;

        public DedupStoreHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
            Initialize(credentials);
        }

        public DedupStoreHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
            Initialize(credentials);
        }

        public DedupStoreHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
            Initialize(credentials);
        }

        public DedupStoreHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
            Initialize(credentials);
        }

        public DedupStoreHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            Initialize(null);
        }

        public bool RouteViaQueryString { get; set; }
        public int TotalPublishRetryCount { get; set; } = 1;

        private void Initialize(VssCredentials credentials)
        {
            if (credentials != null)
            {
                var messageHandler = new VssHttpMessageHandler(credentials, new VssHttpRequestSettings());
                proxyHttpClient = new HttpClient(messageHandler, disposeHandler: true);
            }
        }

        public override Guid ResourceId
        {
            get { return ResourceIds.ChunkResourceId; }
        }

        public async Task<MaybeCached<DedupCompressedBuffer>> GetChunkAsync(ChunkDedupIdentifier dedupId, bool canRedirect, CancellationToken cancellationToken)
        {
            return MaybeCached.FromUncached(await GetDedupAsync(ResourceIds.ChunkResourceId, dedupId, canRedirect, cancellationToken).ConfigureAwait(false));
        }

        public async Task<MaybeCached<DedupCompressedBuffer> > GetNodeAsync(NodeDedupIdentifier dedupId, bool canRedirect, CancellationToken cancellationToken)
        {
            return MaybeCached.FromUncached(await GetDedupAsync(ResourceIds.NodeResourceId, dedupId, canRedirect, cancellationToken).ConfigureAwait(false));
        }

        public async Task<Dictionary<DedupIdentifier, PreauthenticatedUri>> GetDedupUrlsAsync(
            ISet<DedupIdentifier> dedupIds,   
            EdgeCache edgeCache,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string> rawData = await GetDedupUrlsRawAsync(dedupIds, edgeCache, cancellationToken).ConfigureAwait(false);
            return rawData.ToDictionary(
                kvp => DedupIdentifier.Deserialize(kvp.Key),
                kvp => new PreauthenticatedUri(new Uri(kvp.Value), EdgeType.Unknown));
        }

        public async Task<Dictionary<DedupIdentifier, GetDedupAsyncFunc>> GetDedupGettersAsync(
            ISet<DedupIdentifier> dedupIds,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string> json = await GetDedupUrlsRawAsync(dedupIds, edgeCache, cancellationToken).ConfigureAwait(false);
            return json.ToDictionary<KeyValuePair<string, string>, DedupIdentifier, GetDedupAsyncFunc>(
                kvp => DedupIdentifier.Create(kvp.Key),
                kvp => (async (ct) =>
                {
                    DedupCompressedBuffer buffer;
                    if (proxyUri != null)
                    {
                        Uri proxyUriWithQueryString = ProxyUriHelper.GetProxyDownloadUri(blobId: BlobIdentifier.Deserialize(kvp.Key), sasUri: new Uri(kvp.Value), proxyUri: proxyUri, blobServiceUri: this.BaseAddress);
                        buffer = await HandleRedirectAsync(knownToBeCompressed: false, redirect: proxyUriWithQueryString, httpClient: proxyHttpClient, cancellationToken: ct).ConfigureAwait(false);
                    }
                    else
                    {
                        buffer = await HandleRedirectAsync(knownToBeCompressed: false, redirect: new Uri(kvp.Value), httpClient: null, cancellationToken: ct).ConfigureAwait(false);
                    }
                    return MaybeCached.FromUncached(buffer);
                }));
        }

        private async Task<Dictionary<string, string>> GetDedupUrlsRawAsync(
            ISet<DedupIdentifier> dedupIds,
            EdgeCache edgeCache,
            CancellationToken cancellationToken)
        {
            var args = new Dictionary<string, string>();
            if (edgeCache == EdgeCache.Allowed)
            {
                args["allowEdge"] = "true";
            }

            var response = await SendAsync(
                HttpMethod.Post,
                ResourceIds.DedupUrlsResourceId,                
                content: JsonSerializer.SerializeToContent(dedupIds.Select(d => d.ValueString).ToArray()),
                queryParameters: args,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
        }

        public async Task<KeepUntilReceipt> PutChunkAndKeepUntilReferenceAsync(
            ChunkDedupIdentifier dedupId,
            DedupCompressedBuffer chunkBuffer,
            KeepUntilBlobReference keepUntil,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.PutDedupAsync(ResourceIds.ChunkResourceId, dedupId, chunkBuffer, keepUntil, null, cancellationToken).ConfigureAwait(false);
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<KeepUntilReceipt>(result);
            }
            catch (VssServiceResponseException serviceResponse) when (serviceResponse.InnerException is ArgumentException)
            {
                throw serviceResponse.InnerException;
            }
        }

        public async Task<PutNodeResponse> PutNodeAndKeepUntilReferenceAsync(
            NodeDedupIdentifier dedupId,
            DedupCompressedBuffer chunkBuffer,
            KeepUntilBlobReference keepUntil,
            SummaryKeepUntilReceipt receipt,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.PutDedupAsync(ResourceIds.NodeResourceId, dedupId, chunkBuffer, keepUntil, receipt, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = JsonSerializer.Deserialize<Dictionary<string, KeepUntilReceipt>>(content);
                    return new PutNodeResponse(new DedupNodeUpdated(result.ToDictionary(r => DedupIdentifier.Create(r.Key), r => r.Value)));
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var result = JsonSerializer.Deserialize<DedupNodeChildrenNeedAction>(content);
                    return new PutNodeResponse(result);
                }
                else
                { 
                    throw new InvalidOperationException();
                }
            }
            catch (VssServiceResponseException serviceResponse) when (serviceResponse.InnerException is ArgumentException)
            {
                throw serviceResponse.InnerException;
            }
        }

        public async Task<KeepUntilReceipt> TryKeepUntilReferenceChunkAsync(
            ChunkDedupIdentifier dedupId,
            KeepUntilBlobReference keepUntil,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.TryReferenceDedupAsync(ResourceIds.ChunkResourceId, dedupId, keepUntil, null, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<KeepUntilReceipt>(result);
                }
                else
                {
                    // TODO improve this
                    throw new InvalidOperationException();
                }
            }
            catch(DedupNotFoundException)
            {
                return null;
            }
        }

        public async Task<TryReferenceNodeResponse> TryKeepUntilReferenceNodeAsync(
            NodeDedupIdentifier dedupId,
            KeepUntilBlobReference keepUntil,
            SummaryKeepUntilReceipt receipt,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.TryReferenceDedupAsync(ResourceIds.NodeResourceId, dedupId, keepUntil, receipt, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<Dictionary<string, KeepUntilReceipt>>(content);
                    return new TryReferenceNodeResponse(new DedupNodeUpdated(result.ToDictionary(r => DedupIdentifier.Create(r.Key), r => r.Value)));
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonSerializer.Deserialize<DedupNodeChildrenNotEnoughKeepUntil>(content);
                    return new TryReferenceNodeResponse(result);
                }
                else
                {
                    // TODO improve this
                    throw new InvalidOperationException();
                }
            }
            catch (DedupNotFoundException)
            {
                return new TryReferenceNodeResponse(new DedupNodeNotFound());
            };
        }

        public async Task PostEchoAsync(byte[] echoBytes, bool hash, bool base64, bool echo, bool vsoHash, bool storeInBlobStore, CancellationToken cancellationToken)
        {
            HttpContent byteArrayContent = new ByteArrayContent(echoBytes);
            byteArrayContent.Headers.ContentLength = echoBytes.Length;
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            byteArrayContent.Headers.ContentRange = new ContentRangeHeaderValue(echoBytes.Length);

            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("hash", hash.ToString());
            queryParameters.Add("base64", base64.ToString());
            queryParameters.Add("echo", echo.ToString());
            queryParameters.Add("vsoHash", vsoHash.ToString());
            queryParameters.Add("storeInBlobStore", storeInBlobStore.ToString());

            var response = await SendAsync(
                HttpMethod.Post,
                ResourceIds.EchoResourceId,
                content: byteArrayContent,
                queryParameters: queryParameters,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (echo)
            {
                byte[] echoed;
                if (base64)
                {
                    echoed = Convert.FromBase64String(await response.Content.ReadAsStringAsync().EnforceCancellation(cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    echoed = await response.Content.ReadAsByteArrayAsync().EnforceCancellation(cancellationToken).ConfigureAwait(false);
                }

                if (!ByteArrayComparer.ArraysEqual(echoBytes, echoed))
                {
                    throw new Exception("Echoed content is not the same.");
                }
            }
        }
        private async Task<HttpResponseMessage> TryReferenceDedupAsync(Guid locationId, DedupIdentifier dedupId, KeepUntilBlobReference keepUntil, SummaryKeepUntilReceipt receipt, CancellationToken cancellationToken)
        {
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("keepUntil", keepUntil.KeepUntilString);
            if (this.RouteViaQueryString)
            {
                queryParameters.Add("dedupId", dedupId.ValueString);
            }

            var headers = CreateHeadersFromReceipts(receipt);

            var msg = await this.CreateRequestMessageAsync(
                HttpMethod.Post,
                headers,
                locationId,
                routeValues: this.RouteViaQueryString ? null : new { dedupId = dedupId.ValueString },
                queryParameters: queryParameters).ConfigureAwait(false);

            return await SendAsync(msg, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public int RecommendedChunkCountPerCall => 64;

        public async Task<Dictionary<ChunkDedupIdentifier, KeepUntilReceipt>> PutChunksAsync(Dictionary<ChunkDedupIdentifier, DedupCompressedBuffer> allChunks, KeepUntilBlobReference keepUntil, CancellationToken cancellationToken)
        {
            var results = new Dictionary<ChunkDedupIdentifier, KeepUntilReceipt>(allChunks.Count);

            foreach (var chunkPage in allChunks.GetPages(pageSize: RecommendedChunkCountPerCall))
            {
                var response = await AsyncHttpRetryHelper.InvokeAsync<HttpResponseMessage>(
                    () =>
                    {
                        var additionalHeaders = new List<KeyValuePair<string, string>>();

                        // DedupCompressedBuffer caches the compressed array, so we're not compressing twice actually.
                        int totalSize = chunkPage.Sum(chunk =>
                        {
                            ArraySegment<byte> chunkWireBytes;
                            bool isCompressed;
                            chunk.Value.GetBytes(out isCompressed, out chunkWireBytes);
                            return chunkWireBytes.Count;
                        });

                        var wireBytes = new byte[totalSize];
                        int offset = 0;
                        foreach (var chunk in chunkPage)
                        {
                            ArraySegment<byte> chunkWireBytes;
                            bool isCompressed;
                            chunk.Value.GetBytes(out isCompressed, out chunkWireBytes);
                            Buffer.BlockCopy(chunkWireBytes.Array, chunkWireBytes.Offset, wireBytes, offset, chunkWireBytes.Count);
                            string isCompressedString = isCompressed ? "true" : "false";
                            additionalHeaders.Add($"X-ms-chunk-{chunk.Key.ValueString}", $"{chunkWireBytes.Count}/{isCompressedString}");
                            offset += chunkWireBytes.Count;
                        }

                        HttpContent byteArrayContent = new ByteArrayContent(wireBytes);
                        byteArrayContent.Headers.ContentLength = wireBytes.Length;
                        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        byteArrayContent.Headers.ContentRange = new ContentRangeHeaderValue(wireBytes.Length);

                        var queryParameters = new Dictionary<string, string>();
                        queryParameters.Add("keepUntil", keepUntil.KeepUntilString);

                        // retry on OperationCanceledExceptions

                        return SendAsync(
                            HttpMethod.Put,
                            additionalHeaders: additionalHeaders,
                            locationId: ResourceIds.ChunkResourceId,
                            queryParameters: queryParameters,
                            content: byteArrayContent,
                            cancellationToken: cancellationToken);
                    },
                    maxRetries: this.TotalPublishRetryCount,
                    tracer: this.tracer,
                    cancellationToken: cancellationToken,
                    continueOnCapturedContext: false,
                    context: nameof(PutChunksAsync))
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                foreach (var kvp in JsonSerializer.Deserialize<Dictionary<string, KeepUntilReceipt>>(content))
                {
                    results.Add(
                        DedupIdentifier.Create(kvp.Key).CastToChunkDedupIdentifier(),
                        kvp.Value);
                }
            }

            return results;
        }

        public async Task PutRootAsync(
            DedupIdentifier dedupId,
            IdBlobReference rootRef,
            CancellationToken cancellationToken)
        {
            await this.PutRootAsync(ResourceIds.RootResourceId, dedupId, rootRef, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteRootAsync(
            DedupIdentifier dedupId,
            IdBlobReference rootRef,
            CancellationToken cancellationToken)
        {
            await this.DeleteRootAsync(ResourceIds.RootResourceId, dedupId, rootRef, cancellationToken).ConfigureAwait(false);
        }

        public async Task<DedupDownloadInfo> GetDownloadInfoAsync(DedupIdentifier dedupId, bool includeChunks, CancellationToken cancellationToken)
        {
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("includeChunks", includeChunks.ToString());
            if (this.RouteViaQueryString)
            {
                queryParameters.Add("dedupId", dedupId.ValueString);
            }

            var msg = await this.CreateRequestMessageAsync(
                HttpMethod.Get,
                ResourceIds.DedupUrlsResourceId,
                routeValues: this.RouteViaQueryString ? null : new { dedupId = dedupId.ValueString },
                version: new ApiResourceVersion(new Version(major: 5, minor: 0)),
                queryParameters: queryParameters).ConfigureAwait(false);

            var response = await SendAsync(msg, cancellationToken: cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<DedupDownloadInfo>(content);
        }

        public async Task<IList<DedupDownloadInfo>> GetBatchDownloadInfoAsync(ISet<DedupIdentifier> dedupIds, bool includeChunks, CancellationToken cancellationToken)
        {
            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("includeChunks", includeChunks.ToString());
            ISet<string> dedupIdStrSet = dedupIds.Select(d => d.ValueString).ToHashSet(x => x);
            HttpContent postContent = JsonSerializer.SerializeToContent(new DedupIdBatch() { DedupIds = dedupIdStrSet });

            HttpRequestMessage msg = await this.CreateRequestMessageAsync(
                HttpMethod.Post,
                ResourceIds.DedupUrlsBatchResourceId,
                content: postContent,
                version: new ApiResourceVersion(new Version(major: 5, minor: 0)),
                queryParameters: queryParameters).ConfigureAwait(false);

            HttpResponseMessage response = await SendAsync(msg, cancellationToken: cancellationToken).ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<DedupDownloadInfo>>(responseContent);
        }

        private Task<HttpResponseMessage> PutRootAsync(
            Guid locationId,
            DedupIdentifier dedupId,
            IdBlobReference rootRef,
            CancellationToken cancellationToken)
        {
            // retry on OperationCanceledExceptions
            return AsyncHttpRetryHelper.InvokeAsync<HttpResponseMessage>(
                async () =>
                {
                    var queryParameters = new Dictionary<string, string>();
                    if (this.RouteViaQueryString)
                    {
                        queryParameters.Add("dedupId", dedupId.ValueString);
                        queryParameters.Add("name", rootRef.Name);
                    }
                    queryParameters.Add("scope", rootRef.Scope);

                    HttpRequestMessage msg = await this.CreateRequestMessageAsync(
                        HttpMethod.Put,
                        locationId,
                        queryParameters: queryParameters,
                        routeValues: this.RouteViaQueryString ? null : new { dedupId = dedupId.ValueString, name = rootRef.Name }).ConfigureAwait(false);

                    return await SendAsync(msg, cancellationToken: cancellationToken).ConfigureAwait(false);
                },
                maxRetries: this.TotalPublishRetryCount,
                tracer: this.tracer,
                cancellationToken: cancellationToken,
                continueOnCapturedContext: false,
                context: nameof(PutRootAsync));
        }

        private async Task<HttpResponseMessage> DeleteRootAsync(
            Guid locationId,
            DedupIdentifier dedupId,
            IdBlobReference rootRef,
            CancellationToken cancellationToken)
        {
            var queryParameters = new Dictionary<string, string>();
            if (this.RouteViaQueryString)
            {
                queryParameters.Add("dedupId", dedupId.ValueString);
                queryParameters.Add("name", rootRef.Name);
            }
            queryParameters.Add("scope", rootRef.Scope);

            HttpRequestMessage msg = await this.CreateRequestMessageAsync(
                HttpMethod.Delete,
                locationId,
                queryParameters: queryParameters,
                routeValues: this.RouteViaQueryString ? null : new { dedupId = dedupId.ValueString, name = rootRef.Name }).ConfigureAwait(false);

            return await SendAsync(msg, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> PutDedupAsync(Guid locationId, DedupIdentifier dedupId, DedupCompressedBuffer chunkBuffer, KeepUntilBlobReference keepUntil, SummaryKeepUntilReceipt receipt, CancellationToken cancellationToken)
        {
            ArraySegment<byte> wireBytes;
            bool isCompressed;
            chunkBuffer.GetBytesTryCompress(out isCompressed, out wireBytes);
            HttpContent byteArrayContent = new ByteArrayContent(wireBytes.Array, wireBytes.Offset, wireBytes.Count);
            byteArrayContent.Headers.ContentLength = wireBytes.Count;
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            byteArrayContent.Headers.ContentRange = new ContentRangeHeaderValue(wireBytes.Count);

            if (isCompressed)
            {
                byteArrayContent.Headers.ContentEncoding.Add(DedupConstants.XpressCompressionHeaderString);
            }

            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("keepUntil", keepUntil.KeepUntilString);
            if (this.RouteViaQueryString)
            {
                queryParameters.Add("dedupId", dedupId.ValueString);
            }

            var headers = CreateHeadersFromReceipts(receipt);

            var msg = await this.CreateRequestMessageAsync(
                HttpMethod.Put,
                headers,
                locationId,
                queryParameters: queryParameters,
                routeValues: this.RouteViaQueryString ? null : new { dedupId = dedupId.ValueString },
                content: byteArrayContent).ConfigureAwait(false);

            return await SendAsync(msg, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static Dictionary<string, string> CreateHeadersFromReceipts(SummaryKeepUntilReceipt receipt)
        {
            Dictionary<string, string> headers = null;
            if (receipt != null)
            {
                headers = new Dictionary<string, string>();
                headers.Add("X-MS-KeepUntils", string.Join(",", receipt.KeepUntils.Select(k => k?.KeepUntilString ?? string.Empty)));
                headers.Add("X-MS-Signature", Convert.ToBase64String(receipt.Signature));
            }

            return headers;
        }

        private static bool IsAzureTableUri(Uri uri)
        {
            return uri.Host.EndsWith(".table.core.windows.net", StringComparison.OrdinalIgnoreCase);
        }

        private Task<HttpResponseMessage> GetRedirectResponseAsync(Uri redirect, HttpClient httpClient, CancellationToken cancellationToken)
        {
            var servicePoint = ServicePointManager.FindServicePoint(redirect);
            servicePoint.UpdateServicePointSettings(ServicePointConstants.MaxConnectionsPerProc64);


            return AsyncHttpRetryHelper.InvokeAsync<HttpResponseMessage>(
                async () =>
                {
                    Interlocked.Increment(ref calls);
                    var request = CreateRequest(redirect);

                    var client = httpClient ?? basicClient;

                    var responseMessage = await client.SendAsync(request, cancellationToken)
                        .EnforceCancellation(cancellationToken, () => $"Timed out waiting for response for {redirect.ToString()}.")
                        .ConfigureAwait(false);

                    if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                    {
                        return responseMessage;
                    }

                    if (responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        Interlocked.Increment(ref throttledCalls);
                        throw new AsyncHttpRetryHelper.RetryableException("HTTP 503 throttling.");
                    }

                    responseMessage.EnsureSuccessStatusCode();

                    IEnumerable<string> xCache;
                    if (responseMessage.Headers.TryGetValues("X-Cache", out xCache))
                    {
                        Interlocked.Add(ref xCacheHits, xCache.Count(h => h.StartsWith("HIT")));
                        Interlocked.Add(ref xCacheMisses, xCache.Count(h => h.StartsWith("MISS")));
                    }

                    return responseMessage;
                },
                canRetryDelegate: e => e is HttpRequestException,
                maxRetries: 5,
                tracer: this.tracer,
                cancellationToken: cancellationToken,
                continueOnCapturedContext: false,
                context: redirect.AbsoluteUri);
        }

        internal static HttpRequestMessage CreateRequest(Uri redirect)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, redirect);

            if (IsAzureTableUri(redirect))
            {
                request.Headers.Add("Accept", "application/json;odata=minimalmetadata");
            }

            return request;
        }

        internal static async Task<DedupCompressedBuffer> ReadResponseAsync(bool knownToBeCompressed, Uri redirect, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            bool isCompressed;
            int chunkSize;
            var wireBuffer = ChunkerHelper.BorrowChunkBuffer();
            if (IsAzureTableUri(redirect))
            {
                string chunkJson = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                var row = JsonSerializer.Deserialize<ContentTableRow>(chunkJson);
                byte[] rawBytes0 = Convert.FromBase64String(row.Content00);
                chunkSize = rawBytes0.Length;
                Buffer.BlockCopy(rawBytes0, 0, wireBuffer.Value, 0, rawBytes0.Length);
                if (row.Content01.Length > 0)
                {
                    byte[] rawBytes1 = Convert.FromBase64String(row.Content01);
                    Buffer.BlockCopy(rawBytes1, 0, wireBuffer.Value, chunkSize, rawBytes1.Length);
                    chunkSize += rawBytes1.Length;
                }
                isCompressed = row.IsCompressed;
            }
            else
            {
                isCompressed = knownToBeCompressed | responseMessage.Content.Headers.ContentEncoding.Contains(DedupConstants.XpressCompressionHeaderString);
                using (var stream = (await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false)).WrapWithCancellationEnforcement(redirect.AbsoluteUri))
                {
                    int bytesRead;
                    chunkSize = 0;
                    while (0 != (bytesRead = await stream.ReadAsync(wireBuffer.Value, chunkSize, ChunkerHelper.MaxChunkSizeInBytes - chunkSize).ConfigureAwait(false)))
                    {
                        chunkSize += bytesRead;
                    }
                }
            }

            if (isCompressed)
            {
                return DedupCompressedBuffer.FromCompressed(wireBuffer, 0, chunkSize);
            }
            else
            {
                return DedupCompressedBuffer.FromUncompressed(wireBuffer, 0, chunkSize);
            }
        }

        private Task<DedupCompressedBuffer> HandleRedirectAsync(bool knownToBeCompressed, Uri redirect, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return AsyncHttpRetryHelper.InvokeAsync<DedupCompressedBuffer>(
                async () =>
                {
                    using (var timeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                    using (var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken))
                    {
                        var responseMessage = await GetRedirectResponseAsync(redirect, httpClient, combinedSource.Token).ConfigureAwait(false);
                        if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                        {
                            return null;
                        }

                        return await ReadResponseAsync(knownToBeCompressed, redirect, responseMessage, combinedSource.Token).ConfigureAwait(false);
                    }
                },
                canRetryDelegate: null, //defaults are sufficient,
                maxRetries: 5,
                tracer: this.tracer,
                cancellationToken: cancellationToken,
                continueOnCapturedContext: false,
                context: redirect.AbsoluteUri);

        }

        private async Task<DedupCompressedBuffer> GetDedupAsync(Guid locationId, DedupIdentifier dedupId, bool canRedirect, CancellationToken cancellationToken)
        {
            ArgumentUtility.CheckForNull(dedupId, nameof(dedupId));

            var queryParameters = new Dictionary<string, string>();
            queryParameters.Add("redirect", canRedirect.ToString().ToLowerInvariant());

            if (this.RouteViaQueryString)
            {
                queryParameters.Add("dedupId", dedupId.ValueString);
            }

            HttpResponseMessage responseMessage;
            try
            {
                responseMessage = await GetAsync(
                    locationId: locationId,
                    routeValues: this.RouteViaQueryString ? null : new { dedupId = dedupId.ValueString },
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (DedupNotFoundException)
            {
                return null;
            }

            bool isCompressed = responseMessage.Content.Headers.ContentEncoding.Contains(DedupConstants.XpressCompressionHeaderString);

            Uri redirect = responseMessage.Headers.Location;

            if (redirect != null)
            {
                return await HandleRedirectAsync(isCompressed, redirect, httpClient: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var chunkBuffer = ChunkerHelper.BorrowChunkBuffer();
            int rawLength = (int)responseMessage.Content.Headers.ContentLength.Value;
            using (var rawStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var cancellableStream = rawStream.WrapWithCancellationEnforcement(dedupId.ValueString))
            {
                await cancellableStream.ReadToEntireBufferAsync(new ArraySegment<byte>(chunkBuffer.Value, 0, rawLength), cancellationToken).ConfigureAwait(false);
            }

            if (isCompressed)
            {
                return DedupCompressedBuffer.FromCompressed(chunkBuffer, 0, rawLength);
            }
            else
            {
                return DedupCompressedBuffer.FromUncompressed(chunkBuffer, 0, rawLength);
            }
        }

        protected override bool ShouldThrowError(HttpResponseMessage response)
        {
            if(response.StatusCode == HttpStatusCode.SeeOther)
            {
                return false;
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return false;
            }

            return base.ShouldThrowError(response);
        }

        private class ContentTableRow
        {
            public string Content00 { get; set; } // First (up to) 64 KB of content
            public string Content01 { get; set; } // Rest of bytes
            public bool IsCompressed { get; set; }
        }
    }
}
 
