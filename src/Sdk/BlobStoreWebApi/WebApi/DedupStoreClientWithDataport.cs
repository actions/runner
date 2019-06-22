using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;
using Microsoft.DataDeduplication.Interop;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.Content.Common;
using DedupIdentifier = GitHub.Services.BlobStore.Common.DedupIdentifier;
using ChunkDedupIdentifier = GitHub.Services.BlobStore.Common.ChunkDedupIdentifier;

namespace GitHub.Services.BlobStore.WebApi
{
    [CLSCompliant(false)]
    public class DedupStoreClientWithDataport : DedupStoreClient, IDedupStoreClientWithDataport
    {
        public DedupStoreClientWithDataport(IDedupStoreHttpClient client, int maxParallelism)
            : base(client, maxParallelism)
        {
        }

        public async Task DownloadToFileAsync(
            IDedupDataPort dataport,
            DedupIdentifier dedupId,
            string fullPath,
            ulong fileSize,
            GetDedupAsyncFunc dedupFetcher,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken)
        {
            if (fileSize == 0)
            {
                await base.DownloadToFileAsync(dedupId, fullPath, dedupFetcher, proxyUri, edgeCache, cancellationToken).ConfigureAwait(false);
                return;
            }

            dedupFetcher = dedupFetcher ?? (GetDedupAsyncFunc)((ct) => base.GetDedupAsync(dedupId, ct));

            if (dedupId.AlgorithmId == ChunkDedupIdentifier.ChunkAlgorithmId)
            {
                if (dataport == null)
                {
                    await base.DownloadToFileAsync(dedupId, fullPath, dedupFetcher, proxyUri, edgeCache, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var hash = new DedupHash() { Hash = dedupId.AlgorithmResult };
                    if (!await dataport.ContainsChunkAsync(hash))
                    {
                        MaybeCached<DedupCompressedBuffer> dedupBuffer = await dedupFetcher(cancellationToken).ConfigureAwait(false);
                        using (dedupBuffer.Value)
                        {
                            await dataport.InsertChunkAsync(dedupId.CastToChunkDedupIdentifier(), dedupBuffer.Value).ConfigureAwait(false);
                        }
                    }

                    var node = new DedupNode(new ChunkInfo(0, (uint)fileSize, hash.Hash));
                    await dataport.WriteStreamAsync(node, fullPath.Substring(2) /* make volume relative */).ConfigureAwait(false);
                }
            }
            else
            {
                MaybeCached<DedupCompressedBuffer> dedupBuffer = await dedupFetcher(cancellationToken);
                using (dedupBuffer.Value)
                {
                    DedupNode node = DedupNode.Deserialize(dedupBuffer.Value.Uncompressed);
                    await DownloadToFileAsync(dataport, node, fullPath, proxyUri, edgeCache, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<DedupNode> DownloadToFileAsync(IDedupDataPort dataport, DedupNode node, string fullPath, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken)
        {
            if (dataport != null)
            {
                node = await GetFilledNodesAsync(node, proxyUri, edgeCache, cancellationToken);
                await EnsureChunksAreLocalAsync(dataport, node.EnumerateChunkLeafsInOrder().Distinct(), proxyUri, edgeCache, cancellationToken);
                await dataport.WriteStreamAsync(node, fullPath.Substring(2) /* make volume relative */);
            }
            else
            {
                await this.DownloadToFileAsync(node, fullPath, proxyUri, edgeCache, cancellationToken);
            }

            return node;
        }

        public async Task EnsureChunksAreLocalAsync(IDedupDataPort dataPort, IEnumerable<DedupNode> chunks, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken)
        {
            await Task.WhenAll(chunks.GetPages(1000).Select(chunkPage => Task.Run(() => EnsureChunksAreLocalPageAsync(dataPort, chunkPage, proxyUri, edgeCache, cancellationToken))));
        }

        private async Task EnsureChunksAreLocalPageAsync(IDedupDataPort dataPort, IReadOnlyList<DedupNode> chunkPage, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var disposables = new ConcurrentBag<IDisposable>();
            try
            {
                foreach (var dedupId in chunkPage.Select(n => n.GetDedupId()).OrderBy(n => n))
                {
                    disposables.Add(await currentOperations.Acquire(dedupId));
                }

                var chunkHashes = chunkPage.Select(c => new DedupHash() { Hash = c.Hash });
                var lookupResults = await dataPort.ContainsChunksAsync(chunkHashes);
                var misses = new HashSet<DedupIdentifier>();
                foreach (var chunk in chunkPage)
                {
                    if (lookupResults[new DedupHash() { Hash = chunk.Hash }])
                    {
                        Interlocked.Add(ref dedupDownloadBytesSaved, (long)chunk.TransitiveContentBytes);
                    }
                    else
                    {
                        misses.Add(new ChunkDedupIdentifier(chunk.Hash));
                    }
                }

                if (misses.Any())
                {
                    Dictionary<DedupIdentifier, GetDedupAsyncFunc> childDownloaders;
                    using (await AcquireParallelismTokenAsync())
                    {
                        childDownloaders = await this.Client.GetDedupGettersAsync(misses, proxyUri, edgeCache, cancellationToken);
                    }

                    var chunkBufferTasks = childDownloaders
                                            .ToDictionary(
                                                kvp => kvp.Key,
                                                kvp => Task.Run(async () =>
                                                {
                                                    using (await AcquireParallelismTokenAsync())
                                                    {
                                                        var maybeCached = await kvp.Value(cancellationToken);
                                                        UpdateCountersOfDownload(maybeCached, DedupNode.NodeType.ChunkLeaf);
                                                        disposables.Add(maybeCached.Value);
                                                        return maybeCached.Value;
                                                    }
                                                }));

                    await Task.WhenAll(chunkBufferTasks.Values);

                    var chunkBuffers = chunkBufferTasks.ToDictionary(
                        kvp => new ChunkDedupIdentifier(new HashAndAlgorithm(kvp.Key.Value)),
                        kvp => kvp.Value.Result);
                    await dataPort.InsertChunksAsync(chunkBuffers);
                }
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

    }
}
