using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Utils;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.BlobStore.Common.Telemetry;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using ChunkDedupIdentifier = GitHub.Services.BlobStore.Common.ChunkDedupIdentifier;
using DedupIdentifier = GitHub.Services.BlobStore.Common.DedupIdentifier;
using NodeDedupIdentifier = GitHub.Services.BlobStore.Common.NodeDedupIdentifier;

namespace GitHub.Services.BlobStore.WebApi
{
    [CLSCompliant(false)]
    public class DedupStoreClient : IDedupStoreClient
    {
        protected readonly LockSet<DedupIdentifier> currentOperations = new LockSet<DedupIdentifier>();
        public IDedupStoreHttpClient Client { get; }
        protected readonly SemaphoreSlim maxParallelism;

        public int MaxParallelismCount { get; }
        // This should be new every time its called to ensure it's the latest data.
        public DedupDownloadStatistics DownloadStatistics =>
            new DedupDownloadStatistics(chunksDownloaded, compressionDownloadBytesSaved, dedupDownloadBytesSaved, nodesDownloaded, physicalContentBytesDownloaded);

        protected long physicalContentBytesDownloaded;
        protected long compressionDownloadBytesSaved;
        protected long dedupDownloadBytesSaved;
        protected long chunksDownloaded;
        protected long nodesDownloaded;

        public DedupStoreClient(IDedupStoreHttpClient client, int maxParallelism)
        {
            this.Client = client;
            this.MaxParallelismCount = maxParallelism;
            this.maxParallelism = new SemaphoreSlim(maxParallelism, maxParallelism);
        }
        public void ResetDownloadStatistics()
        {
            this.physicalContentBytesDownloaded = 0;
            this.compressionDownloadBytesSaved = 0;
            this.dedupDownloadBytesSaved = 0;
            this.chunksDownloaded = 0;
            this.nodesDownloaded = 0;
        }

        public async Task<IDisposable> AcquireParallelismTokenAsync()
        {
            return await SemaphoreSlimToken.Wait(maxParallelism);
        }

        public IDedupUploadSession CreateUploadSession(
            IDedupStoreClient client,
            KeepUntilBlobReference keepUntilReference,
            IAppTraceSource tracer,
            IFileSystem fileSystem)
        {
            return new UploadSession(client, keepUntilReference, tracer, fileSystem);
        }

        public class UploadSession : IDedupUploadSession
        {
            public long TotalContentBytes { get { return logicalContentBytesUploaded + dedupUploadBytesSaved; } }
            // This should be new every time its called to ensure it's the latest data.
            public DedupUploadStatistics UploadStatistics =>
                new DedupUploadStatistics(chunksUploaded, compressionBytesSaved, dedupUploadBytesSaved, logicalContentBytesUploaded, physicalContentBytesUploaded);

            private long logicalContentBytesUploaded;
            private long physicalContentBytesUploaded;
            private long compressionBytesSaved;
            private long dedupUploadBytesSaved;
            private long chunksUploaded;

            protected readonly ConcurrentDictionary<DedupIdentifier, string> filePaths = new ConcurrentDictionary<DedupIdentifier, string>();
            private readonly ConcurrentDictionary<DedupIdentifier, KeepUntilReceipt> sessionReceipts = new ConcurrentDictionary<DedupIdentifier, KeepUntilReceipt>();
            private readonly LockSet<DedupIdentifier> putNodeInProgress = new LockSet<DedupIdentifier>();
            private readonly LockSet<DedupIdentifier> putChunkInProgress = new LockSet<DedupIdentifier>();
            private readonly IDedupStoreClient client;
            private readonly KeepUntilBlobReference keepUntilReference;
            private readonly IAppTraceSource Tracer;
            private readonly SemaphoreSlim chunkUploadsToBuffer = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            protected readonly ConcurrentDictionary<NodeDedupIdentifier, DedupNode> allNodes = new ConcurrentDictionary<NodeDedupIdentifier, DedupNode>();
            private readonly Lazy<IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier>> parentLookup;
            protected readonly IFileSystem fileSystem;

            public IReadOnlyDictionary<NodeDedupIdentifier, DedupNode> AllNodes => (IReadOnlyDictionary<NodeDedupIdentifier, DedupNode>)allNodes;
            public IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier> ParentLookup => (IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier>)parentLookup.Value;

            public UploadSession(IDedupStoreClient client, KeepUntilBlobReference keepUntilReference, IAppTraceSource tracer, IFileSystem fileSystem)
            {
                this.client = client;
                this.keepUntilReference = keepUntilReference;
                this.Tracer = tracer;
                this.parentLookup = new Lazy<IReadOnlyDictionary<DedupIdentifier, NodeDedupIdentifier>>(
                    () => ProofHelper.CreateParentLookup(AllNodes),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                this.fileSystem = fileSystem;
            }

            public virtual Task<KeepUntilReceipt> UploadAsync(DedupNode node, IReadOnlyDictionary<DedupIdentifier, string> filePaths, CancellationToken cancellationToken)
            {
                foreach (var path in filePaths)
                {
                    this.filePaths[path.Key] = path.Value;
                }

                foreach (var n in node.EnumerateInnerNodesDepthFirst())
                {
                    allNodes[n.CalculateNodeDedupIdentifier()] = n;
                }

                return UploadNodeAsync(node, null, null, null, "", false, cancellationToken);
            }

            private async Task<KeepUntilReceipt> UploadNodeAsync(DedupNode node, string path, Lazy<FileStream> file, long? offset, string indent,
                bool alreadyLocked, CancellationToken cancellationToken)
            {
                var nodeId = new NodeDedupIdentifier(node.Hash);
                KeepUntilReceipt receipt;
                if (sessionReceipts.TryGetValue(nodeId, out receipt))
                {
                    Interlocked.Add(ref dedupUploadBytesSaved, (long)node.TransitiveContentBytes);
                }
                else
                {
                    receipt = await UploadNodeAsyncInternal(nodeId, node, file, path, offset, indent, null, cancellationToken);
                }

                return receipt;
            }

            private sealed class Disposer : IDisposable
            {
                private Dictionary<IDisposable, string> disposables;
                private readonly Action<string> logger;

                public Disposer(Action<string> logger = null)
                {
                    this.logger = logger;
                    this.disposables = null;
                }

                public T Add<T>(T disposable) where T : IDisposable
                {
                    disposables = disposables ?? new Dictionary<IDisposable, string>();
                    disposables.Add(disposable, null);
                    return disposable;
                }

                public T Add<T>(T disposable, string label) where T : IDisposable
                {
                    disposables = disposables ?? new Dictionary<IDisposable, string>();
                    disposables.Add(disposable, label);
                    return disposable;
                }

                public void Dispose()
                {
                    if (disposables != null)
                    {
                        foreach (var disposable in disposables)
                        {
                            disposable.Key.Dispose();
                            if (logger != null & disposable.Value != null)
                            {
                                logger(disposable.Value);
                            }
                        }

                        disposables = null;
                    }
                }
            }

            private async Task<KeepUntilReceipt> UploadNodeAsyncInternal(
                NodeDedupIdentifier nodeId,
                DedupNode node,
                Lazy<FileStream> file,
                string path,
                long? offset,
                string indent,
                KeepUntilReceipt[] receipts,
                CancellationToken cancellationToken)
            {
                using (var bufferDisposer = new Disposer())
                {
                    var serializedNode = bufferDisposer.Add(DedupCompressedBuffer.FromUncompressed(node.Serialize()));

                    var childIds = node.ChildNodes.Select(n => n.GetDedupId()).ToList();

                    var distinctChildrenAndParent = childIds.Concat(new[] { nodeId }).Distinct().ToList();
                    distinctChildrenAndParent.Sort();

                    PutNodeResponse response;
                    SummaryKeepUntilReceipt summaryReceipt;
                    using (var putNodeDisposer = new Disposer(msg => Tracer.Verbose(msg)))
                    {
                        foreach (var dedupId in distinctChildrenAndParent)
                        {
                            //Tracer.Verbose($"UploadNodeAsyncInternal {nodeId.ValueString} waiting for: {dedupId.ValueString}");
                            putNodeDisposer.Add(await putNodeInProgress.Acquire(dedupId));//, $"UploadNodeAsyncInternal {nodeId.ValueString} released: {dedupId.ValueString}");
                            //Tracer.Verbose($"UploadNodeAsyncInternal {nodeId.ValueString} acquired: {dedupId.ValueString}");
                        }

                        receipts = receipts ?? new KeepUntilReceipt[node.ChildNodes.Count];
                        for (int i = 0; i < receipts.Length; i++)
                        {
                            KeepUntilReceipt receipt;
                            if (receipts[i] == null && this.sessionReceipts.TryGetValue(childIds[i], out receipt))
                            {
                                receipts[i] = receipt;
                            }
                        }

                        summaryReceipt = receipts.Any(r => r != null)
                            ? new SummaryKeepUntilReceipt(receipts)
                            : null;

                        using (await this.client.AcquireParallelismTokenAsync())
                        {
                            var childNodesStr = new List<string>();
                            for (int i = 0; i < node.ChildNodes.Count; i++)
                            {
                                childNodesStr.Add($"{node.ChildNodes[i].GetDedupId()} [{receipts[i]?.KeepUntil.KeepUntilString ?? "None"}]");
                            }

                            Tracer.Verbose("{0}Trying to put node {1} of {2} children, {3} receipts used. (Children: {4})",
                                indent,
                                nodeId.ValueString,
                                node.ChildNodes.Count,
                                receipts.Count(r => r != null),
                                string.Join(", ", childNodesStr));
                            response = await this.client.Client.PutNodeAndKeepUntilReferenceAsync(
                                nodeId,
                                serializedNode,
                                keepUntilReference,
                                summaryReceipt,
                                cancellationToken);
                        }
                    }

                    var result = await response.Match(
                        async childrenAction =>
                        {
                            Tracer.Verbose("{0}Could not add node {1} of {2} children as {3} children are missing and {4} children have insufficient keepuntil. Got {5} receipts back though. (Missing: {6}; InsufficientKeepUntil: {7}; Receipts: {8})", indent,
                                nodeId.ValueString,
                                node.ChildNodes.Count,
                                childrenAction.Missing.Count(),
                                childrenAction.InsufficientKeepUntil.Count(),
                                childrenAction.Receipts.Count(),
                                string.Join(", ", childrenAction.Missing.Select(c => c.ValueString)),
                                string.Join(", ", childrenAction.InsufficientKeepUntil.Select(c => c.ValueString)),
                                string.Join(", ", childrenAction.Receipts.Select(kvp => $"{kvp.Key.ValueString} [{kvp.Value.KeepUntil.KeepUntilString}]")));

                            KeepUntilReceipt[] childReceipts = await UploadChildren(node, receipts, childrenAction, file, path, offset, indent, cancellationToken);

                            for (int i = 0; i < childIds.Count; i++)
                            {
                                Debug.Assert(childReceipts[i] != null, "There should be a receipt for each child.");
                                sessionReceipts[childIds[i]] = childReceipts[i];
                            }

                            summaryReceipt = new SummaryKeepUntilReceipt(childReceipts);

                            using (await this.client.AcquireParallelismTokenAsync())
                            {
                                var childNodesStr = new List<string>();
                                for (int i = 0; i < node.ChildNodes.Count; i++)
                                {
                                    childNodesStr.Add(
                                        $"{node.ChildNodes[i].GetDedupId()} [{childReceipts[i]?.KeepUntil.KeepUntilString ?? "None"}]");
                                }

                                Tracer.Verbose("{0}After uploading children, trying to put node {1} of {2} children, {3} receipts used. (Children: {4})",
                                    indent,
                                    nodeId.ValueString,
                                    node.ChildNodes.Count,
                                    childReceipts.Count(r => r != null),
                                    string.Join(", ", childNodesStr));

                                var response2 = await this.client.Client.PutNodeAndKeepUntilReferenceAsync(
                                    nodeId,
                                    serializedNode,
                                    keepUntilReference,
                                    summaryReceipt,
                                    cancellationToken);

                                return response2.Match(
                                    stillChildrenAction => { throw new InvalidOperationException(); },
                                    added =>
                                    {
                                        Tracer.Verbose("{0}After uploading children, added node {1} of {2} children and got {3} receipts back. (Receipts: {4})",
                                            indent,
                                            nodeId.ValueString,
                                            node.ChildNodes.Count,
                                            added.Receipts.Count,
                                            string.Join(", ", added.Receipts.Select(kvp => $"{kvp.Key.ValueString} [{kvp.Value.KeepUntil.KeepUntilString}]")));
                                        foreach (var receipt in added.Receipts)
                                        {
                                            sessionReceipts[receipt.Key] = receipt.Value;
                                        }

                                        return added.Receipts[nodeId];
                                    });
                            }
                        },
                        added =>
                        {
                            Tracer.Verbose("{0}Added node {1} of {2} children and got {3} receipts back. (Receipts: {4})",
                                indent,
                                nodeId.ValueString,
                                node.ChildNodes.Count,
                                added.Receipts.Count,
                                string.Join(", ", added.Receipts.Select(kvp => $"{kvp.Key.ValueString} [{kvp.Value.KeepUntil.KeepUntilString}]")));
                            Interlocked.Add(ref dedupUploadBytesSaved, (long)node.TransitiveContentBytes);
                            foreach (var receipt in added.Receipts)
                            {
                                sessionReceipts[receipt.Key] = receipt.Value;
                            }
                            return Task.FromResult(added.Receipts[nodeId]);
                        }
                    );
                    return result;
                }
            }

            private async Task<KeepUntilReceipt[]> UploadChildren(
                DedupNode node,
                KeepUntilReceipt[] existingReceipts,
                DedupNodeChildrenNeedAction childrenAction,
                Lazy<FileStream> file,
                string path,
                long? offset,
                string indent,
                CancellationToken cancellationToken)
            {
                var nodeId = node.GetNodeId();

                var childUploadTasks = new Dictionary<DedupIdentifier, Task<KeepUntilReceipt>>();
                var childChunksToUpload = new Dictionary<ChunkDedupIdentifier, DedupCompressedBuffer>();
                var newReceipts = new ConcurrentDictionary<DedupIdentifier, KeepUntilReceipt>();

                Debug.Assert((path != null) == offset.HasValue);
                Debug.Assert((file != null) == offset.HasValue);
                Lazy<FileStream> fileToClose = null;
                if (path == null && this.filePaths.ContainsKey(nodeId))
                {
                    path = this.filePaths[nodeId];
                    offset = 0;
                    fileToClose = file = new Lazy<FileStream>(() => this.fileSystem.OpenFileStreamForAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
                }
                Debug.Assert((path != null) == offset.HasValue);
                Debug.Assert((file != null) == offset.HasValue);

                try
                {
                    IDisposable chunkUploadToken = await chunkUploadsToBuffer.WaitToken();
                    try
                    {
                        for (int i = 0; i < node.ChildNodes.Count; i++)
                        {
                            DedupNode childNode = node.ChildNodes[i];

                            var dedupId = DedupIdentifier.Create(childNode);

                            if (existingReceipts[i] != null && !childUploadTasks.ContainsKey(dedupId))
                            {
                                childUploadTasks.Add(dedupId, Task.FromResult(existingReceipts[i]));
                            }

                            //Tracer.Verbose("{0}Examining child {1} consisting of {2:N0} bytes...", indent, dedupId, childNode.TransitiveContentBytes);

                            if (childUploadTasks.ContainsKey(dedupId))
                            {
                                Interlocked.Add(ref dedupUploadBytesSaved, (long)childNode.TransitiveContentBytes);
                            }
                            else if (childrenAction.Missing.Contains(dedupId) || childrenAction.InsufficientKeepUntil.Contains(dedupId))
                            {
                                if (childNode.Type == DedupNode.NodeType.ChunkLeaf)
                                {
                                    var chunkId = (ChunkDedupIdentifier)dedupId;

                                    KeepUntilReceipt keepUntilReceipt;
                                    if (sessionReceipts.TryGetValue(chunkId, out keepUntilReceipt))
                                    {
                                        Interlocked.Add(ref dedupUploadBytesSaved, (long)childNode.TransitiveContentBytes);
                                        childUploadTasks.Add(dedupId, Task.FromResult(keepUntilReceipt));
                                    }
                                    else if (!childChunksToUpload.ContainsKey(chunkId))
                                    {
                                        DedupCompressedBuffer dedupBuffer;
                                        int chunkSize = (int)childNode.TransitiveContentBytes;

                                        string chunkPath;
                                        long chunkOffset;
                                        FileStream chunkFile;
                                        if (offset.HasValue)
                                        {
                                            chunkPath = path;
                                            chunkFile = file.Value;
                                            chunkOffset = offset.Value;
                                        }
                                        else
                                        {
                                            chunkPath = this.filePaths[chunkId];
                                            chunkOffset = 0;
                                            chunkFile =
                                                chunkPath.EndsWith(FileBlobDescriptorConstants.EmptyDirectoryEndingPattern) ?
                                                    null : // There's nothing to open stream on.
                                                    this.fileSystem.OpenFileStreamForAsync(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                                        }

                                        try
                                        {
                                            using (await this.client.AcquireParallelismTokenAsync())
                                            {
                                                var chunkBuffer = ChunkerHelper.BorrowChunkBuffer();
                                                int bytesRead =
                                                    chunkFile == null && chunkPath.EndsWith(FileBlobDescriptorConstants.EmptyDirectoryEndingPattern) ?
                                                        0 : // There's nothing to read.
                                                        await AsyncFile.ReadAsync(chunkFile, chunkOffset, chunkBuffer.Value, chunkSize);

                                                if (bytesRead != chunkSize)
                                                {
                                                    throw new EndOfStreamException("Could not read entire chunk.");
                                                }

                                                dedupBuffer = DedupCompressedBuffer.FromUncompressed(chunkBuffer, 0, chunkSize);

                                                bool isCompressed;
                                                ArraySegment<byte> wireBytes;
                                                dedupBuffer.GetBytesTryCompress(out isCompressed, out wireBytes);

                                                Tracer.Verbose("{0}Uploading chunk {1} consisting of {2:N0} bytes ({3}) from {4}@{5}...",
                                                    indent,
                                                    dedupId,
                                                    chunkSize,
                                                    isCompressed ? $"compressed to {wireBytes.Count:N0}" : "not compressed",
                                                    chunkPath,
                                                    chunkOffset);
                                            }
                                        }
                                        finally
                                        {
                                            if (chunkFile != null && (file == null || !file.IsValueCreated || chunkFile != file.Value))
                                            {
                                                chunkFile.Dispose();
                                            }
                                        }

                                        childChunksToUpload.Add((ChunkDedupIdentifier)dedupId, dedupBuffer);
                                    }
                                }
                                else
                                {
                                    var childTask = UploadNodeAsync(childNode, path, file, offset, indent + " ", true, cancellationToken);
                                    childUploadTasks.Add(dedupId, childTask);
                                }
                            }
                            else
                            {
                                Interlocked.Add(ref dedupUploadBytesSaved, (long)childNode.TransitiveContentBytes);

                                // Use what is returned from the server, as that could be a better KeepUntil datetime; if not present, fall back to our own.
                                KeepUntilReceipt receipt;
                                if (!childrenAction.Receipts.TryGetValue(dedupId, out receipt))
                                {
                                    receipt = existingReceipts[i]; // existingReceipts is aligned with ChildNodes
                                }

                                childUploadTasks.Add(dedupId, Task.FromResult(receipt));
                            }

                            if (offset.HasValue)
                            {
                                offset += (long)childNode.TransitiveContentBytes;
                            }
                        }

                        await Task.WhenAll(childChunksToUpload.GetPages(pageSize: 128).Select(page => Task.Run(async () =>
                        {
                            page.Sort((kvp1, kvp2) => ByteArrayComparer.Instance.Compare(kvp1.Key.Value, kvp2.Key.Value));

                            using (var chunkUploadDisposer = new Disposer(msg => Tracer.Verbose(msg)))
                            {
                                var chunksToUpload = new HashSet<DedupIdentifier>();
                                foreach (DedupIdentifier dedupId in page.Select(kvp => kvp.Key))
                                {
                                    KeepUntilReceipt existingReceipt;
                                    if (this.sessionReceipts.TryGetValue(dedupId, out existingReceipt))
                                    {
                                        newReceipts.TryAdd(dedupId, existingReceipt);
                                        continue;
                                    }
                                    //Tracer.Verbose($"UploadChildren {nodeId.ValueString} waiting for: {dedupId.ValueString}");
                                    chunkUploadDisposer.Add(await putChunkInProgress.Acquire(dedupId));//, $"UploadChildren {nodeId.ValueString} released: {dedupId.ValueString}");
                                                                                                       //Tracer.Verbose($"UploadChildren {nodeId.ValueString} acquired: {dedupId.ValueString}");
                                    if (this.sessionReceipts.TryGetValue(dedupId, out existingReceipt))
                                    {
                                        newReceipts.TryAdd(dedupId, existingReceipt);
                                    }
                                    else
                                    {
                                        chunksToUpload.Add(dedupId);
                                    }
                                }

                                page = page.Where(kvp => chunksToUpload.Contains(kvp.Key)).ToList();
                                using (await this.client.AcquireParallelismTokenAsync())
                                {
                                    Tracer.Verbose("{0}Uploading {1} chunks consisting of {2:N0} bytes...",
                                        indent, page.Count, page.Sum(c => c.Value.Uncompressed.Count));
                                    var results = await this.client.Client.PutChunksAsync(
                                        page.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                                        keepUntilReference,
                                        cancellationToken);

                                    foreach (var kvp in page)
                                    {
                                        ArraySegment<byte>? wireBytes;
                                        var isCompressed = kvp.Value.TryGetAlreadyCompressed(out wireBytes);
                                        Interlocked.Add(ref logicalContentBytesUploaded, kvp.Value.Uncompressed.Count);
                                        Interlocked.Add(ref physicalContentBytesUploaded, isCompressed ? wireBytes.Value.Count : kvp.Value.Uncompressed.Count);
                                        Interlocked.Add(ref compressionBytesSaved, isCompressed ? kvp.Value.Uncompressed.Count - wireBytes.Value.Count : 0);
                                        Interlocked.Increment(ref chunksUploaded);

                                        kvp.Value.Dispose();
                                    }

                                    foreach (var result in results)
                                    {
                                        newReceipts.TryAdd(result.Key, result.Value);
                                        this.sessionReceipts.TryAdd(result.Key, result.Value);
                                    }

                                    Tracer.Verbose("{0}Uploaded {1} chunks consisting of {2:N0} bytes.",
                                        indent, page.Count, page.Sum(c => c.Value.Uncompressed.Count));
                                }
                            }
                        })));
                    }
                    finally
                    {
                        foreach (var buffer in childChunksToUpload.Values)
                        {
                            buffer.Dispose();
                        }
                        chunkUploadToken.Dispose();
                    }

                    await Task.WhenAll(childUploadTasks.Values);
                }
                finally
                {
                    if (fileToClose != null && fileToClose.IsValueCreated)
                    {
                        fileToClose.Value.Dispose();
                    }
                }

                var receipts = new KeepUntilReceipt[node.ChildNodes.Count];
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    var dedupId = DedupIdentifier.Create(node.ChildNodes[i]);
                    KeepUntilReceipt receipt;
                    if (newReceipts.TryGetValue(dedupId, out receipt))
                    {
                        receipts[i] = receipt;
                    }
                    else
                    {
                        receipts[i] = await childUploadTasks[dedupId];
                    }
                }

                return receipts;
            }
        }

        public async Task<MaybeCached<DedupCompressedBuffer>> GetNodeAsync(NodeDedupIdentifier nodeId, CancellationToken cancellationToken)
        {
            using (await currentOperations.Acquire(nodeId))
            using (await AcquireParallelismTokenAsync())
            {
                var bytes = await this.Client.GetNodeAsync(nodeId, canRedirect: true, cancellationToken: cancellationToken);
                UpdateCountersOfDownload(bytes, DedupNode.NodeType.InnerNode);
                return bytes;
            }
        }

        public async Task<MaybeCached<DedupCompressedBuffer>> GetChunkAsync(ChunkDedupIdentifier chunkId, CancellationToken cancellationToken)
        {
            using (await currentOperations.Acquire(chunkId))
            using (await AcquireParallelismTokenAsync())
            {
                var bytes = await this.Client.GetChunkAsync(chunkId, canRedirect: true, cancellationToken: cancellationToken);
                UpdateCountersOfDownload(bytes, DedupNode.NodeType.ChunkLeaf);
                return bytes;
            }
        }

        public Task<MaybeCached<DedupCompressedBuffer>> GetDedupAsync(DedupIdentifier dedupId, CancellationToken cancellationToken)
        {
            if (dedupId.AlgorithmId == ChunkDedupIdentifier.ChunkAlgorithmId)
            {
                return this.GetChunkAsync(dedupId.CastToChunkDedupIdentifier(), cancellationToken);
            }
            else
            {
                return this.GetNodeAsync(dedupId.CastToNodeDedupIdentifier(), cancellationToken);
            }
        }

        public async Task DownloadToFileAsync(
            DedupIdentifier dedupId,
            string fullPath,
            GetDedupAsyncFunc dedupFetcher,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken)
        {
            MaybeCached<DedupCompressedBuffer> dedupBuffer;
            if (dedupFetcher != null)
            {
                dedupBuffer = await dedupFetcher(cancellationToken);
            }
            else
            {
                dedupBuffer = await this.GetDedupAsync(dedupId, cancellationToken);
            }

            using (dedupBuffer.Value)
            {
                if (dedupId.AlgorithmId == ChunkDedupIdentifier.ChunkAlgorithmId)
                {
                    UpdateCountersOfDownload(dedupBuffer, DedupNode.NodeType.ChunkLeaf);
                    await WriteChunkToFileAsync(fullPath, dedupBuffer);
                    return;
                }

                DedupNode node = DedupNode.Deserialize(dedupBuffer.Value.Uncompressed.CreateCopy());
                await DownloadToFileAsync(node, fullPath, proxyUri, edgeCache, cancellationToken);
            }
        }

        protected async Task WriteChunkToFileAsync(string fullPath, MaybeCached<DedupCompressedBuffer> dedupBuffer)
        {
            using (FileStream file = OpenForWrite(fullPath))
            {
                var chunkBuffer = dedupBuffer.Value.Uncompressed;
                await file.WriteAsync(chunkBuffer.Array, chunkBuffer.Offset, chunkBuffer.Count);
            }
        }

        protected FileStream OpenForWrite(string fullPath, FileOptions fileOptions = FileOptions.None)
        {
            Func<FileStream> openStreamInternal = () => FileStreamUtils.OpenFileStreamForAsync(
                            fullPath, FileMode.Create, FileAccess.Write, FileShare.Write, fileOptions);
            try
            {
                return openStreamInternal();
            }
            catch (UnauthorizedAccessException)
            {
                File.Delete(fullPath);
                return openStreamInternal();
            }
        }

        public async Task<DedupNode> DownloadToFileAsync(DedupNode node, string fullPath, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken)
        {
            node = await GetFilledNodesAsync(node, proxyUri, edgeCache, cancellationToken);
            string tempFilePath = Path.Combine(Path.GetDirectoryName(fullPath), Guid.NewGuid().ToString() + ".tmp");
            using (FileStream wholeFile = OpenForWrite(tempFilePath, FileOptions.DeleteOnClose))
            {
                bool sparseFile = false;

                if (Environment.GetEnvironmentVariable("VSTS_SPARSE_FILES") == "1")
                {
                    int error;
                    if (0 != (error = AsyncFile.TryMarkSparse(wholeFile.SafeFileHandle, sparse: true)))
                    {
                        throw new IOException("Could not mark file as sparse: " + error);
                    }

                    sparseFile = true;
                }

                long fileLength = (long)node.TransitiveContentBytes;
                wholeFile.SetLength(fileLength);

                var fileRangeDownloads = NonSwallowingActionBlock.Create<IEnumerable<Tuple<DedupNode, long>>>(
                    async chunksAndOffsets =>
                    {
                        var distinctChunkIds = new HashSet<DedupIdentifier>(chunksAndOffsets.Select(t => t.Item1.GetChunkId()));
                        Dictionary<DedupIdentifier, GetDedupAsyncFunc> fetchers;
                        using (await AcquireParallelismTokenAsync())
                        {
                            fetchers = await this.Client.GetDedupGettersAsync(distinctChunkIds, proxyUri, edgeCache, cancellationToken);
                        }

                        var fetchBlock = NonSwallowingTransformBlock.Create<
                            Tuple<DedupNode, long>,
                            Tuple<DedupCompressedBuffer, long>>
                            (
                            async chunkAndOffset =>
                            {
                                using (await AcquireParallelismTokenAsync())
                                {
                                    var maybeCached = await fetchers[chunkAndOffset.Item1.GetDedupId()](cancellationToken);
                                    UpdateCountersOfDownload(maybeCached, DedupNode.NodeType.ChunkLeaf);
                                    if ((ulong)maybeCached.Value.Uncompressed.Count != chunkAndOffset.Item1.TransitiveContentBytes)
                                    {
                                        throw new EndOfStreamException($"Dedup size does not match the downloaded size. DedupId: {chunkAndOffset.Item1.GetDedupId()}");
                                    }

                                    return Tuple.Create(maybeCached.Value, chunkAndOffset.Item2);
                                }
                            },
                            new ExecutionDataflowBlockOptions()
                            {
                                BoundedCapacity = this.MaxParallelismCount + 2,
                                MaxDegreeOfParallelism = this.MaxParallelismCount,
                                CancellationToken = cancellationToken,
                            });

                        var writerBlock = NonSwallowingActionBlock.Create<Tuple<DedupCompressedBuffer, long>>(
                            async chunkBufferAndOffset =>
                            {
                                var chunkFileOffset = chunkBufferAndOffset.Item2;
                                using (var chunkBuffer = chunkBufferAndOffset.Item1)
                                using (await AcquireParallelismTokenAsync())
                                {
                                    chunkBuffer.AssertValid();
                                    var segment = chunkBuffer.Uncompressed;
                                    await AsyncFile.WriteAsync(wholeFile, chunkFileOffset, segment)
                                        .EnforceCancellation(cancellationToken, () => $"Timed out waiting for WriteAsync to '{wholeFile.Name}'.");
                                }
                            },
                            new ExecutionDataflowBlockOptions()
                            {
                                BoundedCapacity = 2,
                                MaxDegreeOfParallelism = 1, //Hmm??
                                CancellationToken = cancellationToken,
                            });

                        fetchBlock.LinkTo(writerBlock, new DataflowLinkOptions() { PropagateCompletion = true });

                        await fetchBlock.SendAllAndCompleteAsync(chunksAndOffsets, writerBlock, cancellationToken);
                    },
                    new ExecutionDataflowBlockOptions()
                    {
                        MaxDegreeOfParallelism = this.MaxParallelismCount,
                        BoundedCapacity = this.MaxParallelismCount + 2,
                        CancellationToken = cancellationToken,
                    });

                long fileOffset = 0;
                foreach (var page in node.EnumerateChunkLeafsInOrder().GetPages(pageSize: 1000))
                {
                    var chunksAndOffsets = new List<Tuple<DedupNode, long>>(page.Count);
                    foreach (var chunk in page)
                    {
                        chunksAndOffsets.Add(Tuple.Create(chunk, fileOffset));
                        fileOffset += (long)chunk.TransitiveContentBytes;
                    }
                    await fileRangeDownloads.SendOrThrowSingleBlockNetworkAsync(chunksAndOffsets, cancellationToken);
                }
                fileRangeDownloads.Complete();
                await fileRangeDownloads.Completion;

                if (sparseFile)
                {
                    int error;
                    if (0 != (error = AsyncFile.TryMarkSparse(wholeFile.SafeFileHandle, sparse: false)))
                    {
                        throw new IOException("Could not unmark file as sparse: " + error);
                    }
                }

                FileLink.CreateHardLinkStatus status = FileLink.CreateHardLink(tempFilePath, fullPath);
                if (status != FileLink.CreateHardLinkStatus.Success)
                {
                    // Hard linking fails if the file already exists.
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                    FileLink.CreateHardLinkStatus finalStatus = FileLink.CreateHardLink(tempFilePath, fullPath);
                    if (finalStatus != FileLink.CreateHardLinkStatus.Success)
                    {
                        throw new IOException($"Hard linking failed! \n Status: {finalStatus.ToString()} \n Path: {fullPath}");
                    }
                }
            }

            return node;
        }

        public async Task<DedupNode> GetFilledNodesAsync(DedupNode node, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken)
        {
            if (node.Type == DedupNode.NodeType.ChunkLeaf)
            {
                return node;
            }

            if (node.ChildNodes == null)
            {
                using (DedupCompressedBuffer nodeBuffer = await this.GetNodeAsync(new NodeDedupIdentifier(node.Hash), cancellationToken))
                {
                    var nodeWithChildren = DedupNode.Deserialize(nodeBuffer.Uncompressed.CreateCopy());
                    node = nodeWithChildren;
                }
            }

            var childNodeDedupIds = new HashSet<DedupIdentifier>(node.ChildNodes.Where(n => n.Type == DedupNode.NodeType.InnerNode).Select(DedupIdentifier.Create));
            if (!childNodeDedupIds.Any())
            {
                return node;
            }

            Dictionary<DedupIdentifier, GetDedupAsyncFunc> childNodeDownloaders;
            using (await AcquireParallelismTokenAsync())
            {
                childNodeDownloaders = await this.Client.GetDedupGettersAsync(childNodeDedupIds, proxyUri, edgeCache, cancellationToken);
            }

            var childNodes = childNodeDownloaders
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Task.Run(async () =>
                    {
                        DedupNode childNode;
                        using (await currentOperations.Acquire(kvp.Key))
                        using (await AcquireParallelismTokenAsync())
                        {
                            var nodeBuffer = await kvp.Value(cancellationToken);
                            using (nodeBuffer.Value)
                            {
                                UpdateCountersOfDownload(nodeBuffer, DedupNode.NodeType.InnerNode);
                                childNode = DedupNode.Deserialize(nodeBuffer.Value.Uncompressed);
                            }
                        }

                        return await GetFilledNodesAsync(childNode, proxyUri, edgeCache, cancellationToken);
                    }));

            var filledChildren = new List<DedupNode>();
            foreach (var child in node.ChildNodes)
            {
                var nodeId = new NodeDedupIdentifier(child.Hash);
                if (childNodes.ContainsKey(nodeId))
                {
                    filledChildren.Add(await childNodes[nodeId]);
                }
                else
                {
                    filledChildren.Add(child);
                }
            }
            return new DedupNode(filledChildren);

        }

        private async Task<DedupNode> WriteToStreamAsync(
            TryAddValueAsyncFunc<DedupCompressedBuffer> writeBufferAsync,
            long nodeFileOffset,
            DedupNode node,
            GetDedupAsyncFunc nodeFetcher,
            Uri proxyUri,
            EdgeCache edgeCache,
            CancellationToken cancellationToken)
        {
            if (node.Type == DedupNode.NodeType.ChunkLeaf)
            {
                var chunkBuffer = await GetChunkAsync(new ChunkDedupIdentifier(node.Hash), cancellationToken);
                await writeBufferAsync(chunkBuffer, cancellationToken);
                return node;
            }

            if (node.ChildNodes == null)
            {
                DedupCompressedBuffer nodeBuffer;
                if (nodeFetcher == null)
                {
                    nodeBuffer = await this.GetNodeAsync(new NodeDedupIdentifier(node.Hash), cancellationToken);
                }
                else
                {
                    var maybeCachedNodeBuffer = await nodeFetcher(cancellationToken);
                    UpdateCountersOfDownload(maybeCachedNodeBuffer, DedupNode.NodeType.InnerNode);
                    nodeBuffer = maybeCachedNodeBuffer.Value;
                }

                using (nodeBuffer)
                {
                    var nodeWithChildren = DedupNode.Deserialize(nodeBuffer.Uncompressed.CreateCopy());
                    node = nodeWithChildren;
                }
            }

            long fileOffset = nodeFileOffset;
            List<KeyValuePair<long, DedupNode>> childOffsets = node.ChildNodes.Select(
                n =>
                {
                    long offset = fileOffset;
                    fileOffset += (long)n.TransitiveContentBytes;
                    return new KeyValuePair<long, DedupNode>(offset, n);
                }).ToList();

            var dedupIds = new HashSet<DedupIdentifier>(node.ChildNodes.Select(DedupIdentifier.Create));
            Dictionary<DedupIdentifier, GetDedupAsyncFunc> childDownloaders;
            using (await AcquireParallelismTokenAsync())
            {
                childDownloaders = await this.Client.GetDedupGettersAsync(dedupIds, proxyUri, edgeCache, cancellationToken);
            }

            // Only download each chunk once via .Distinct()
            var chunks = node.ChildNodes
                .Where(c => c.Type == DedupNode.NodeType.ChunkLeaf)
                .Distinct()
                .ToDictionary(
                    c => c,
                    c => Task.Run(async () =>
                    {
                        var childNodeId = DedupIdentifier.Create(c);
                        var fetcher = childDownloaders[childNodeId];

                        using (await currentOperations.Acquire(childNodeId))
                        using (await AcquireParallelismTokenAsync())
                        {
                            return await fetcher(cancellationToken);
                        }
                    }));

            Dictionary<DedupNode, int> chunkCounts =
                childOffsets
                .GroupBy(
                    keySelector: offsetAndNode => offsetAndNode.Value,
                    elementSelector: offsetAndNode => offsetAndNode.Key)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Count());

            var children = new List<DedupNode>(childOffsets.Count);
            foreach (var offsetAndNode in childOffsets)
            {
                DedupNode childNode = offsetAndNode.Value;
                long childFileOffset = offsetAndNode.Key;

                if (childNode.Type == DedupNode.NodeType.ChunkLeaf)
                {
                    MaybeCached<DedupCompressedBuffer> chunkBuffer = await chunks[childNode];
                    if (chunkCounts[childNode] > 1)
                    {
                        var chunkCopy = ChunkerHelper.BorrowChunkBuffer();
                        Buffer.BlockCopy(
                            chunkBuffer.Value.Uncompressed.Array,
                            chunkBuffer.Value.Uncompressed.Offset,
                            chunkCopy.Value,
                            0,
                            chunkBuffer.Value.Uncompressed.Count);
                        chunkBuffer = MaybeCached.FromCached(DedupCompressedBuffer.FromUncompressed(
                            chunkCopy, 0, chunkBuffer.Value.Uncompressed.Count));
                    }

                    UpdateCountersOfDownload(chunkBuffer, DedupNode.NodeType.ChunkLeaf);
                    await writeBufferAsync(chunkBuffer.Value, cancellationToken);

                    chunkCounts[childNode]--;
                }
                else
                {
                    childNode = await WriteToStreamAsync(writeBufferAsync, childFileOffset, childNode, childDownloaders[childNode.GetDedupId()], proxyUri, edgeCache, cancellationToken);
                }
                children.Add(childNode);
            }

            Debug.Assert(chunkCounts.Values.All(i => i == 0), "Not all chunks were written.");

            var filledNode = new DedupNode(children);
            if (filledNode.Hash.ToHex() != node.Hash.ToHex())
            {
                throw new InvalidOperationException();
            }

            return filledNode;
        }

        protected void UpdateCountersOfDownload(MaybeCached<DedupCompressedBuffer> bytes, DedupNode.NodeType type)
        {
            if (type == DedupNode.NodeType.InnerNode)
            {
                Interlocked.Increment(ref nodesDownloaded);
            }
            else
            {
                if (bytes.Cached)
                {
                    Interlocked.Add(ref dedupDownloadBytesSaved, bytes.Value.Uncompressed.Count);
                    return;
                }

                ArraySegment<byte>? wireBytes;
                bool isCompressed = true;
                if (!bytes.Value.TryGetAlreadyCompressed(out wireBytes))
                {
                    isCompressed = false;
                    wireBytes = bytes.Value.Uncompressed;
                }

                if (isCompressed)
                {
                    Interlocked.Add(ref compressionDownloadBytesSaved, bytes.Value.Uncompressed.Count - wireBytes.Value.Count);
                }

                Interlocked.Increment(ref chunksDownloaded);
                Interlocked.Add(ref physicalContentBytesDownloaded, wireBytes.Value.Count);
            }
        }

        public Task<Dictionary<DedupIdentifier, GetDedupAsyncFunc>> GetDedupGettersAsync(ISet<DedupIdentifier> dedupIds, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken)
        {
            return this.Client.GetDedupGettersAsync(dedupIds, proxyUri, edgeCache, cancellationToken);
        }
    }
}
