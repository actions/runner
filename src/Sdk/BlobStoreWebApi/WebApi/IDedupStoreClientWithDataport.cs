using BuildXL.Cache.ContentStore.Hashing;
using Microsoft.DataDeduplication.Interop;
using GitHub.Services.BlobStore.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DedupIdentifier = GitHub.Services.BlobStore.Common.DedupIdentifier;

namespace GitHub.Services.BlobStore.WebApi
{
    [CLSCompliant(false)]
    public interface IDedupStoreClientWithDataport : IDedupStoreClient
    {
        int MaxParallelismCount { get; }

        Task DownloadToFileAsync(
            IDedupDataPort dataport,
            DedupIdentifier dedupId,
            string fullPath,
            ulong fileSize,
            GetDedupAsyncFunc dedupFetcher,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken);

        Task<DedupNode> DownloadToFileAsync(
            IDedupDataPort dataport,
            DedupNode node,
            string fullPath,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken);

        Task EnsureChunksAreLocalAsync(
            IDedupDataPort dataPort,
            IEnumerable<DedupNode> chunks,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken);
    }
}
