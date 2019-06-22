using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.BlobStore.Common.Telemetry;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using ChunkDedupIdentifier = GitHub.Services.BlobStore.Common.ChunkDedupIdentifier;
using DedupIdentifier = GitHub.Services.BlobStore.Common.DedupIdentifier;
using NodeDedupIdentifier = GitHub.Services.BlobStore.Common.NodeDedupIdentifier;

namespace GitHub.Services.BlobStore.WebApi
{
    [CLSCompliant(false)]
    public interface IDedupStoreClient
    {
        IDedupStoreHttpClient Client { get; }

        DedupDownloadStatistics DownloadStatistics { get; }

        Task<IDisposable> AcquireParallelismTokenAsync();

        IDedupUploadSession CreateUploadSession(
            IDedupStoreClient client,
            KeepUntilBlobReference keepUntilReference,
            IAppTraceSource tracer,
            IFileSystem fileSystem);

        Task DownloadToFileAsync(
            DedupIdentifier dedupId,
            string fullPath,
            GetDedupAsyncFunc dedupFetcher,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken);

        Task<DedupNode> DownloadToFileAsync(DedupNode node, string fullPath, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken);

        Task<MaybeCached<DedupCompressedBuffer>> GetChunkAsync(ChunkDedupIdentifier chunkId, CancellationToken cancellationToken);

        Task<MaybeCached<DedupCompressedBuffer>> GetDedupAsync(DedupIdentifier dedupId, CancellationToken cancellationToken);

        Task<DedupNode> GetFilledNodesAsync(DedupNode node, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken);

        Task<MaybeCached<DedupCompressedBuffer>> GetNodeAsync(NodeDedupIdentifier nodeId, CancellationToken cancellationToken);

        Task<Dictionary<DedupIdentifier, GetDedupAsyncFunc>> GetDedupGettersAsync(
            ISet<DedupIdentifier> dedupIds,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken);

        void ResetDownloadStatistics();
    }

    [CLSCompliant(false)]
    public interface IDedupUploadSession
    {
        IReadOnlyDictionary<NodeDedupIdentifier, DedupNode> AllNodes { get; }
        IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier> ParentLookup { get; }
        DedupUploadStatistics UploadStatistics { get; }

        Task<KeepUntilReceipt> UploadAsync(DedupNode node, IReadOnlyDictionary<DedupIdentifier, string> filePaths, CancellationToken cancellationToken);
    }
}
