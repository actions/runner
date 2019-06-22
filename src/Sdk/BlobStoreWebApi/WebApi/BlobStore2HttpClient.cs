using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.BlobStore.WebApi.Contracts;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.WebApi;
using System.Runtime.Serialization;
using static ServicePointExtensions;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    /// A Vss Http Client for the Blob Store V2
    /// </summary>
    [ResourceArea(areaId: ResourceIds.BlobAreaId)]
    public class BlobStore2HttpClient : ArtifactHttpClient, IBlobStoreHttpClient
    {
        private readonly VssHttpRequestSettings settings;

        #region Constructors
        public BlobStore2HttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
            this.settings = null;
        }

        public BlobStore2HttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
            this.settings = settings;
        }

        public BlobStore2HttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
            this.settings = null;
        }

        public BlobStore2HttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
            this.settings = settings;
        }

        public BlobStore2HttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            // The following reflection loop is heavily inspired by the constructor in
            // https://mseng.visualstudio.com/DefaultCollection/VSOnline/_git/VSO#path=%2FVssf%2FClient%2FCommon%2FVssHttpMessageHandler.cs&version=GBmaster&_a=contents

            HttpMessageHandler handler = pipeline;
            do
            {
                VssHttpMessageHandler messageHandler = handler as VssHttpMessageHandler;
                if (messageHandler != null)
                {
                    messageHandler.Settings.SendTimeout = ServiceToServiceTimeout;
                    break;
                }

                DelegatingHandler delegatingHandler = handler as DelegatingHandler;
                handler = delegatingHandler == null ? null : delegatingHandler.InnerHandler; // TODO6 : delegatingHandler?.InnerHandler;
            }
            while (handler != null);
        }
        #endregion

        protected override ServicePointConfig GetServicePointSettings()
        {
            var config = base.GetServicePointSettings();
            config.ConnectionLeaseTimeout = TimeSpan.FromMinutes(3);
            return config;
        }

        protected string TargetVersion
        {
            get
            {
                return "2.1-preview.1";
            }
        }

        public bool ParallelHttpCallsSupported = true;

        #region IBlobStoreHttpClient

        public virtual async Task<PreauthenticatedUri> GetDownloadUriAsync(BlobIdWithHeaders blobId,
            CancellationToken cancellationToken)
        {
            ArgumentUtility.CheckForNull(blobId, BlobConstants.BlobIdQuery);

            var args = new Dictionary<string, string>();
            if (blobId.FileName != null)
            {
                args["filename"] = blobId.FileName;
            }
            if (blobId.ContentType != null)
            {
                args["contentType"] = blobId.ContentType;
            }
            if (blobId.ExpiryTime.HasValue)
            {
                // Round up to the nearest second as that is the granularity of the formatting.
                DateTimeOffset roundedUp = blobId.ExpiryTime.Value + TimeSpan.FromSeconds(1);
                args["expiryTime"] = roundedUp.ToString(KeepUntilBlobReference.KeepUntilFormat, CultureInfo.InvariantCulture);
            }
            if (blobId.EdgeCache == EdgeCache.Allowed)
            {
                args["allowEdge"] = "true";
            }


            TimeSpan expectedTimeout = this.settings?.SendTimeout ?? TimeSpan.Zero;
            TimeSpan backupTimeout = TimeSpan.FromTicks(2 * expectedTimeout.Ticks);

            string responseBody;
            using (var backupTimeoutCts = backupTimeout.Ticks <= 0 ? new CancellationTokenSource() : new CancellationTokenSource(backupTimeout))
            {
                HttpResponseMessage response = await SendAsync(
                    HttpMethod.Get,
                    locationId: ResourceIds.BlobUrlResourceId,
                    routeValues: new Dictionary<string, object>() { { "blobId", blobId.BlobId.ValueString } },
                    queryParameters: args,
                    version: new ApiResourceVersion(this.TargetVersion),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    //either blob is not found or something wrong happened
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NotFound: throw new BlobNotFoundException($"Blob {blobId.BlobId} not found");
                        default: throw new BlobServiceException($"Something wrong happens when retrieving {blobId.BlobId}, http status {response.StatusCode}");
                    }
                }

                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return new PreauthenticatedUri(new Uri(JsonSerializer.Deserialize<Blob>(responseBody).Url), EdgeType.Unknown);
        }

        public virtual async Task<IDictionary<BlobIdentifier, PreauthenticatedUri>> GetDownloadUrisAsync(
            IEnumerable<BlobIdentifier> blobIds,
            EdgeCache edgeCache,
            CancellationToken cancellationToken,
            DateTimeOffset? expiryTime = null)
        {
            ArgumentUtility.CheckForNull(blobIds, BlobConstants.BlobIdQuery);

            // Optimization:
            // If we have only one URI call the single blob URI endpoint instead.
            var FirstTwo = blobIds.Take(2); // Take the first two to avoid calling Count()
            var actualTaken = FirstTwo.Count();
            if (actualTaken == 1)
            {
                var blobId = FirstTwo.First();
                PreauthenticatedUri uri = await GetDownloadUriAsync(
                    new BlobIdWithHeaders(blobId, edgeCache, expiryTime: expiryTime),
                    cancellationToken).ConfigureAwait(false);

                return new Dictionary<BlobIdentifier, PreauthenticatedUri>
                {
                    { blobId, uri }
                };
            }
            else if (actualTaken == 0)
            {
                return new Dictionary<BlobIdentifier, PreauthenticatedUri>();
            }

            var args = new Dictionary<string, string>();
            if (expiryTime.HasValue)
            {
                args["expiryTime"] = expiryTime.Value.ToString(KeepUntilBlobReference.KeepUntilFormat, CultureInfo.InvariantCulture);
            }
            if (edgeCache == EdgeCache.Allowed)
            {
                args["allowEdge"] = "true";
            }

            var blobsToUris = new ConcurrentDictionary<BlobIdentifier, PreauthenticatedUri>();
            var pages = blobIds.GetPages(DefaultDownloadUriPageSize);

            if (ParallelHttpCallsSupported)
            {
                var taskQueue = NonSwallowingActionBlock.Create<IEnumerable<BlobIdentifier>>(
                    pageOfBlobIds => GetDownloadUrisPageAsync(blobsToUris, pageOfBlobIds, args, cancellationToken),
                    new ExecutionDataflowBlockOptions() {
                        MaxDegreeOfParallelism = MaxParallelGetDownloadUri,
                        CancellationToken = cancellationToken,
                    });

                await taskQueue.PostAllToUnboundedAndCompleteAsync(pages, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var pageOfBlobIds in pages)
                {
                    await GetDownloadUrisPageAsync(blobsToUris, pageOfBlobIds, args, cancellationToken).ConfigureAwait(false);
                }
            }

            return blobsToUris;
        }

        private async Task GetDownloadUrisPageAsync(
            ConcurrentDictionary<BlobIdentifier, PreauthenticatedUri> blobsToUris,
            IEnumerable<BlobIdentifier> pageOfBlobIds,
            Dictionary<string,string> args,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await SendAsync(
                HttpMethod.Post,
                locationId: ResourceIds.BlobBatchResourceId,
                version: new ApiResourceVersion(this.TargetVersion),
                queryParameters: args,
                content: JsonSerializer.SerializeToContent(new BlobBatch(pageOfBlobIds)),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            BlobBatch result = JsonSerializer.Deserialize<BlobBatch>(responseBody);
            foreach (Blob blob in result.Blobs)
            {
                blobsToUris.TryAdd(
                    BlobIdentifier.Deserialize(blob.Id),
                    new PreauthenticatedUri(new Uri(blob.Url), EdgeType.Unknown));
            }
        }

        public Task RemoveReferencesAsync(IDictionary<BlobIdentifier, IEnumerable<IdBlobReference>> referencesGroupedByBlobIds)
        {
            var kvps = referencesGroupedByBlobIds.SelectMany(kvp => kvp.Value.Select(refId => new KeyValuePair<BlobReference, BlobIdentifier>(new BlobReference(refId), kvp.Key)));

            return SendAsync(
                HttpMethod.Delete,
                ResourceIds.ReferenceBatchResourceId,
                version: new ApiResourceVersion(this.TargetVersion),
                routeValues: new { },
                content: JsonSerializer.SerializeToContent(new ReferenceBatch(kvps)));
        }

        public Task<IDictionary<BlobIdentifier, IEnumerable<BlobReference>>> TryReferenceAsync(
            IDictionary<BlobIdentifier, IEnumerable<BlobReference>> referencesGroupedByBlobIds,
            CancellationToken cancellationToken)
        {
            var batch = new ReferenceBatch(referencesGroupedByBlobIds.SelectMany(kvp => kvp.Value.Select(refId => new KeyValuePair<BlobReference, BlobIdentifier>(refId, kvp.Key))));
            return TryReferenceInternalAsync(batch, cancellationToken);
        }

        public Task<IDictionary<BlobIdentifier, IEnumerable<BlobReference>>> TryReferenceWithBlocksAsync(
            IDictionary<BlobIdentifierWithBlocks, IEnumerable<BlobReference>> referencesGroupedByBlobIds,
            CancellationToken cancellationToken)
        {
            var batch = new ReferenceBatch(referencesGroupedByBlobIds.SelectMany(kvp => kvp.Value.Select(refId => new KeyValuePair<BlobReference, BlobIdentifierWithBlocks>(refId, kvp.Key))));
            return TryReferenceInternalAsync(batch, cancellationToken);
        }
        #endregion

        #region Fields
        // Blame HttpRetryHelper for this... code analysis suggests remainingRetries is only decremented when remainingRetries is > 1.
        protected const string GZip = "gzip";
        protected const int DefaultMaxParallelBlockUpload = 64;
        protected const int DefaultDownloadUriPageSize = 5000;

        protected static readonly int MaxParallelGetDownloadUri = int.Parse(Environment.GetEnvironmentVariable("MaxParallelGetDownloadUri") ?? Environment.ProcessorCount.ToString());
        protected static readonly int MaxParallelFileUpload = int.Parse(Environment.GetEnvironmentVariable("MaxParallelFileUpload") ?? Environment.ProcessorCount.ToString());
        protected static readonly int MaxParallelBlockUpload = int.Parse(Environment.GetEnvironmentVariable("MaxParallelBlockUpload") ?? DefaultMaxParallelBlockUpload.ToString());

        internal static readonly SemaphoreSlim BlockUploadSemaphore = new SemaphoreSlim(MaxParallelBlockUpload, MaxParallelBlockUpload);

#if NET_STANDARD
        /// <remarks>
        /// ConfigurationManager is not in .NET standard as of 2019-01-25. It was defaulted to 5 minutes to move class to .NET standard. If there is a need to put the configuration back in for .NET standard, feel free to do so.
        /// </remarks>
        protected static readonly TimeSpan ServiceToServiceTimeout = TimeSpan.FromMinutes(5);
#else
        protected static readonly TimeSpan ServiceToServiceTimeout = (ConfigurationManager.AppSettings["BlobStoreHttpClientServiceToServiceTimeout"] == null)
            ? TimeSpan.FromMinutes(5)
            : TimeSpan.Parse(ConfigurationManager.AppSettings["BlobStoreHttpClientServiceToServiceTimeout"]);
#endif
        #endregion

        /// <summary>
        /// Exceptions for errors
        /// </summary>
        protected override IDictionary<String, Type> TranslatedExceptions => BlobExceptionMapping.ClientTranslatedExceptions;

        #region Public Methods

        /// <summary>
        /// Get a file from the content service using a the supplied blob identifier.
        /// </summary>
        /// <param name="blobId">The globally unique identifier for the blob to download</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that returns the stream of bytes requested</returns>
        public async Task<Stream> GetBlobAsync(BlobIdentifier blobId, CancellationToken cancellationToken)
        {
            ArgumentUtility.CheckForNull(blobId, BlobConstants.BlobIdQuery);

            HttpResponseMessage responseMessage;
            try
            {
                responseMessage = await SendAsync(
                    HttpMethod.Get,
                    locationId: ResourceIds.BlobResourceId,
                    routeValues: new { blobId = blobId.ValueString },
                    version: new ApiResourceVersion(this.TargetVersion),
                    completionOption: HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (BlobNotFoundException)
            {
                return null;
            }

            Stream stream = await responseMessage.Content.ReadAsStreamAsync().EnforceCancellation(cancellationToken).ConfigureAwait(false);

            if (responseMessage.Content.Headers.ContainsContentEncoding(GZip))
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }

            return stream.WrapWithCancellationEnforcement(blobId.ValueString);
        }

        public override Guid ResourceId
        {
            get { return ResourceIds.BlobResourceId; }
        }

        public virtual Task PutBlobBlockAsync(
            BlobIdentifier blobId,
            BlobBlockHash blockHash,
            byte[] blockBuffer,
            int blockLength,
            CancellationToken cancellationToken)
        {
            return UploadBlockAsync(
                blockBuffer,
                blockLength,
                blobId,
                blockHash,
                isOnlyBlock: false,
                reference: null,
                cancellationToken: cancellationToken);
        }

        public virtual Task PutBlobBlockAsync(
            BlobIdentifier blobId,
            byte[] blockBuffer,
            int blockLength,
            CancellationToken cancellationToken)
        {
            return PutBlobBlockAsync(
                blobId,
                VsoHash.HashBlock(blockBuffer, blockLength),
                blockBuffer,
                blockLength,
                cancellationToken);
        }

        public virtual Task PutSingleBlockBlobAndReferenceAsync(
            BlobIdentifier blobId,
            byte[] blockBuffer,
            int blockLength,
            BlobReference reference,
            CancellationToken cancellationToken)
        {
            return UploadBlockAsync(
                blockBuffer,
                blockLength,
                blobId,
                blockHash: null,
                isOnlyBlock: true,
                reference: reference,
                cancellationToken: cancellationToken);
        }

        private struct Void { }

        public async Task<BlobIdentifierWithBlocks> UploadBlocksForBlobAsync(BlobIdentifier blobId, Stream blobStream, CancellationToken cancellationToken)
        {
            ArgumentUtility.CheckForNull(blobStream, "stream");

            // for a given blob, we only need to upload each block once even if the block appears multiple times in a blob.
            var blockUploads = new RunOnce<BlobBlockHash>(consolidateExceptions: true);

            BlobIdentifierWithBlocks blobIdWithBlocks = await VsoHash.WalkAllBlobBlocksAsync(blobStream,
                blockActionSemaphore: BlockUploadSemaphore,
                multiBlocksInParallel: true,
                multiBlockCallback: (block, blockLength, blockHash, isFinalBlock) =>
                {
                    return blockUploads.RunOnceAsync(
                        blockHash, 
                        () => UploadBlockAsync(block, blockLength, blobId, blockHash, isOnlyBlock: false, reference: null, cancellationToken));
                }).ConfigureAwait(false);

            if (blobId != blobIdWithBlocks.BlobId)
            {
                string errorFormat = "BlobId '{0}' does not match BlobIdentifierWithBlocks '{1}' '{2}' computed from blobStream.";
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, errorFormat, blobId, blobIdWithBlocks, blobIdWithBlocks.BlobId));
            }

            return blobIdWithBlocks;
        }

        public async virtual Task<IEnumerable<BlobIdentifierWithBlocks>> UploadBlocksForBlobsAsync(IEnumerable<BlobToUriMapping> pathToUriMappings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var blobIdsWithBlocks = new ConcurrentBag<BlobIdentifierWithBlocks>();
            var taskQueue = NonSwallowingActionBlock.Create<BlobToUriMapping>(
                async pathToUriMapping =>
                {
                    BlobIdentifierWithBlocks blobIdWithBlocks = await UploadBlocksForSingleBlobTaskAsync(pathToUriMapping, cancellationToken).ConfigureAwait(false);
                    if (blobIdWithBlocks != null)
                    {
                        blobIdsWithBlocks.Add(blobIdWithBlocks);
                    }
                },
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = MaxParallelFileUpload, CancellationToken = cancellationToken });
            await taskQueue.PostAllToUnboundedAndCompleteAsync(pathToUriMappings, cancellationToken).ConfigureAwait(false);
            return blobIdsWithBlocks;
        }
#endregion

#region Protected Methods

        protected virtual async Task UploadBlockAsync(
            byte[] block,
            int blockLength,
            BlobIdentifier blobId,
            BlobBlockHash blockHash,
            bool isOnlyBlock,
            BlobReference reference,
            CancellationToken cancellationToken)
        {
            // Upload each block in a retry wrapper. We may get a broken TCP link during the uploading
            // and the retry handler built into the client is unable to catch that error since it only
            // covers until the headers are successfully sent.

            await AsyncHttpRetryHelper.InvokeVoidAsync(
                () =>
                {
                    // set the content and the Content-Range header
                    HttpContent byteArrayContent = new ByteArrayContent(block, 0, blockLength);
                    byteArrayContent.Headers.ContentLength = blockLength;
                    byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    byteArrayContent.Headers.ContentRange = new ContentRangeHeaderValue(blockLength);

                    var queryParameters = new Dictionary<string, string>();
                    if (!isOnlyBlock)
                    {
                        queryParameters.Add("blockHash", blockHash.HashString);
                    }

                    if (reference != null)
                    {
                        reference.Match(
                            idReference =>
                            {
                                if (idReference.Scope != null)
                                {
                                    queryParameters.Add("referenceScope", idReference.Scope);
                                }
                        // reference.Name != null by construction
                        queryParameters.Add("referenceId", idReference.Name);
                            },
                            keepUntilReference =>
                            {
                                queryParameters.Add("keepUntil", keepUntilReference.KeepUntilString);
                            }
                        );
                    }

                    return SendAsync(
                        isOnlyBlock ? HttpMethod.Put : HttpMethod.Post,
                        ResourceIds.BlobResourceId,
                        routeValues: new { blobId = blobId.ValueString },
                        queryParameters: queryParameters,
                        version: new ApiResourceVersion(this.TargetVersion),
                        content: byteArrayContent,
                        cancellationToken: cancellationToken);
                },
                int.Parse(Environment.GetEnvironmentVariable("BLOBSTORE_PUTBLOCK_RETRY_COUNT") ?? "3"),
                this.tracer,
                cancellationToken,
                continueOnCapturedContext: false,
                context: $"{nameof(UploadBlockAsync)} {blobId.ValueString} {blockHash?.HashString ?? "[single block blob]"}").ConfigureAwait(false);
        }

        protected async Task<BlobIdentifierWithBlocks> UploadBlocksForSingleBlobTaskAsync(BlobToUriMapping mapping, CancellationToken cancellationToken)
        {
            try
            {
                using (var stream = mapping.StreamFactory.Value)
                {
                    stream.Position = 0;
                    return await UploadBlocksForBlobAsync(mapping.BlobId, stream, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                exception.Data["ContentSpec"] = mapping.ContentSpec;
                throw;
            }
        }
#endregion

        private async Task<IDictionary<BlobIdentifier, IEnumerable<BlobReference>>> TryReferenceInternalAsync(
            ReferenceBatch batch,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await SendAsync(
                method: HttpMethod.Post,
                locationId: ResourceIds.ReferenceBatchResourceId,
                version: new ApiResourceVersion(this.TargetVersion),
                content: JsonSerializer.SerializeToContent(batch),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ReferenceBatch result = JsonSerializer.Deserialize<ReferenceBatch>(responseBody);
            
            var missing = result.References.Where(r => r.Status.Equals(ReferenceStatus.Missing.ToString(), StringComparison.OrdinalIgnoreCase));
            return missing.GroupBy(r => r.Blob).ToDictionary(k => k.Key.ToBlobIdentifier(), v => v.Select(i => i.BlobReference));
        }

        public struct BlobUrisResponse
        {
            [DataMember]
            public List<BlobIdentifier> BlobIds;

            [DataMember]
            public List<Uri> Uris;
        }
    }
}
