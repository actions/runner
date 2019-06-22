using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BuildXL.Cache.ContentStore.Hashing;
using Microsoft.DataDeduplication.Interop;
using GitHub.Services.ArtifactServices.App.Shared;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.BlobStore.WebApi.Exceptions;
using GitHub.Services.BlobStore.Common.Telemetry;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Telemetry;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.WebApi;
using ChunkDedupIdentifier = GitHub.Services.BlobStore.Common.ChunkDedupIdentifier;
using DedupIdentifier = GitHub.Services.BlobStore.Common.DedupIdentifier;

namespace GitHub.Services.BlobStore.WebApi
{
    [CLSCompliant(false)]
    public class DedupManifestArtifactClient : IDedupManifestArtifactClient
    {
        private const string EnvironmentVariablePrefix = "VSO_BUILD_DROP_";
        private readonly BlobStoreClientTelemetry ClientTelemetry;
        private readonly IDedupStoreClientWithDataport client;
        private readonly IAppTraceSource tracer;
        private readonly IFileSystem fileSystem;
        private readonly TimeSpan DefaultKeepUntilDuration = TimeSpan.FromDays(2);
        private static readonly EdgeCache edgeCache;
        private static readonly bool isWindows = Helpers.IsWindowsPlatform(Environment.OSVersion);

        static DedupManifestArtifactClient()
        {
            edgeCache = EdgeCacheHelper.GetEdgeCacheEnvVar(EnvironmentVariablePrefix);
        }

        public DedupManifestArtifactClient(
            IDedupStoreClientWithDataport client,
            IAppTraceSource tracer) : this(client, tracer, FileSystem.Instance)
        {
        }

        // Constructor to be used by agents with existing telemetry.
        public DedupManifestArtifactClient(
            BlobStoreClientTelemetry blobStoreClientTelemetry,
            IDedupStoreClientWithDataport client,
            IAppTraceSource tracer) : this(blobStoreClientTelemetry, client, tracer, FileSystem.Instance)
        {
        }

        // Construtor in use by old agents, creates a new telemetry instance.
        internal DedupManifestArtifactClient(
            IDedupStoreClientWithDataport client,
            IAppTraceSource tracer,
            IFileSystem fileSystem) : this(new BlobStoreClientTelemetry(tracer, client.Client.BaseAddress), client, tracer, fileSystem)
        {
        }

        internal DedupManifestArtifactClient(
            BlobStoreClientTelemetry blobStoreClientTelemetry,
            IDedupStoreClientWithDataport client,
            IAppTraceSource tracer,
            IFileSystem fileSystem)
        {
            this.ClientTelemetry = blobStoreClientTelemetry;
            this.client = client;
            this.tracer = tracer;
            this.fileSystem = fileSystem;
        }

        public TimeSpan StatsInterval { get; set; } = TimeSpan.FromSeconds(5);

        public Task<PublishResult> PublishAsync(
            string fullPath,
            CancellationToken cancellationToken)
        {
            return this.PublishAsync(fullPath, new ArtifactPublishOptions(), cancellationToken);
        }

        public Task<PublishResult> PublishAsync(
            string fullPath,
            ArtifactPublishOptions artifactPublishOptions,
            CancellationToken cancellationToken)
        {
            return this.PublishAsync(fullPath, artifactPublishOptions, manifestFileOutputPath: null, cancellationToken);
        }

        public async Task<PublishResult> PublishAsync(
            string fullPath,
            ArtifactPublishOptions artifactPublishOptions,
            string manifestFileOutputPath,
            CancellationToken cancellationToken)
        {
            this.Trace_X_TFS_SessionId();
            List<FileInfo> fileInfoList;
            string sourceDirectory;
            if (!this.fileSystem.DirectoryExists(fullPath) && !this.fileSystem.FileExists(fullPath))
            {
                throw new InvalidPathException(BlobStoreResources.InvalidPath());
            }
            else if (this.fileSystem.FileExists(fullPath))
            {
                FileInfo fileInfo = new FileInfo(fullPath);
                fileInfoList = new List<FileInfo> { fileInfo };
                sourceDirectory = Path.GetDirectoryName(fullPath);
            }
            else
            {
                sourceDirectory = fullPath;
                fileInfoList = null;
            }

            bool deleteManifestAfterPublish = false;
            if (manifestFileOutputPath == null)
            {
                string manifestFileName = $"{nameof(DedupManifestArtifactClient)}.{this.fileSystem.GetRandomFileName()}.manifest";
                manifestFileOutputPath = Path.Combine(this.fileSystem.GetTempFullPath(), manifestFileName);
                deleteManifestAfterPublish = true;
            }

            var hashes = new List<FileBlobDescriptor>();
            await new PrecomputedHashesGenerator(tracer, this.fileSystem).PaginateAndProcessFiles(
                sourceDirectory: sourceDirectory,
                filePaths: fileInfoList,
                chunkDedup: true,
                includeEmptyDirectories: true,
                artifactPublishOptions: artifactPublishOptions,
                cancellationToken: cancellationToken,
                hashCompleteCallback: (hash) => { lock (hashes) { hashes.Add(hash); } }).ConfigureAwait(false);
            long fileCount = hashes.Count(h => !h.AbsolutePath.EndsWith(FileBlobDescriptorConstants.EmptyDirectoryEndingPattern));

            hashes.Sort((f1, f2) => StringComparer.Ordinal.Compare(f1.RelativePath, f2.RelativePath));
            var nodes = hashes.Select(b => b.Node).ToList();
            while (nodes.Count > 1)
            {
                nodes = nodes.GetPages(DedupNode.MaxDirectChildrenPerNode).Select(children => new DedupNode(children)).ToList();
            }

            Dictionary<DedupIdentifier, string> filePaths = new Dictionary<DedupIdentifier, string>();
            foreach (var hash in hashes)
            {
                filePaths[hash.Node.GetDedupId()] = hash.AbsolutePath;
            }

            try
            {
                GenerateManifest(hashes, manifestFileOutputPath, cancellationToken);
                var manifest = await FileBlobDescriptor.CalculateAsync(
                    fileSystem: this.fileSystem,
                    rootDirectory: this.fileSystem.GetTempFullPath(),
                    chunkDedup: true,
                    relativePath: manifestFileOutputPath,
                    fileBlobType: FileBlobType.File,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                filePaths[manifest.Node.GetDedupId()] = manifest.AbsolutePath;
                DedupNode contentNode = nodes.Count() > 0 ? nodes.Single() : default;
                DedupNode root = nodes.Count() > 0 ? (new DedupNode(new[] { contentNode, manifest.Node })) : (new DedupNode(new[] { manifest.Node }));

                tracer.Verbose($"ManifestId: {manifest.Node.GetDedupId()}");
                string contentNodeMsg = nodes.Count > 0 ? $"ContentNode: {contentNode.GetDedupId()}" : "ContentNode: Does not exist, it's just an empty directory.";
                tracer.Verbose(contentNodeMsg);
                tracer.Verbose($"RootId: {root.GetDedupId()}");

                KeepUntilBlobReference keepUntil = new KeepUntilBlobReference(DateTime.UtcNow.Add(DefaultKeepUntilDuration));

                tracer.Info($"Uploading {hashes.Count} files from: {fullPath}");

                IDedupUploadSession dedupUploadSession = this.client.CreateUploadSession(client, keepUntil, tracer, this.fileSystem);

                var statsCancelationSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                string transitiveBytesInMb = (NumberConversionHelper.ConvertBytesToMegabytes((long)root.TransitiveContentBytes)).ToString("N1");
                string totalUploadedBytesInMb;
                var reportTask = Task.Run(async () =>
                {
                    while (!statsCancelationSrc.IsCancellationRequested)
                    {
                        totalUploadedBytesInMb = (NumberConversionHelper.ConvertBytesToMegabytes(dedupUploadSession.UploadStatistics.TotalContentBytes)).ToString("N1");
                        tracer.Info($"Uploaded {totalUploadedBytesInMb} MB out of {transitiveBytesInMb} MB.");
                        await Task.Delay(StatsInterval, statsCancelationSrc.Token).ConfigureAwait(false);
                    }
                });

                await dedupUploadSession.UploadAsync(root, filePaths, cancellationToken).ConfigureAwait(false);

                totalUploadedBytesInMb = (NumberConversionHelper.ConvertBytesToMegabytes(dedupUploadSession.UploadStatistics.TotalContentBytes)).ToString("N1");
                tracer.Info($"Uploaded {totalUploadedBytesInMb} MB out of {transitiveBytesInMb} MB.");
                tracer.Info("Upload completed.");
                statsCancelationSrc.Cancel();

                // Send Upload Telemetry
                DedupUploadTelemetryRecord uploadRecord = ClientTelemetry.CreateRecord<DedupUploadTelemetryRecord>((level, uri, prefix) =>
                    new DedupUploadTelemetryRecord(level, uri, prefix, nameof(dedupUploadSession.UploadAsync), dedupUploadSession.UploadStatistics));
                ClientTelemetry.SendRecord(uploadRecord);

                tracer.Info($"{Environment.NewLine}Upload statistics:{Environment.NewLine}" + dedupUploadSession.UploadStatistics.AsString());
                var proofNodes = ProofHelper.CreateProofNodes(
                    dedupUploadSession.AllNodes,
                    dedupUploadSession.ParentLookup,
                    hashes.Select(h => h.Node.GetDedupId()).Concat(new[] { manifest.Node.GetDedupId() }).Distinct());

                var serializedProofNodes = proofNodes.Select(n => Convert.ToBase64String(n.Serialize()));
                return new PublishResult(manifest.Node.GetDedupId(), root.GetDedupId(), serializedProofNodes, fileCount, (long)contentNode.TransitiveContentBytes);
            }
            finally
            {
                if (deleteManifestAfterPublish && this.fileSystem.FileExists(manifestFileOutputPath))
                {
                    try
                    {
                        this.fileSystem.DeleteFile(manifestFileOutputPath);
                    }
                    catch { }
                }
            }
        }

        public Task DownloadAsync(DedupIdentifier manifestId, string targetDirectory, CancellationToken cancellationToken)
        {
            DownloadDedupManifestArtifactOptions downloadOptions = DownloadDedupManifestArtifactOptions.CreateWithManifestId(
                manifestId,
                targetDirectory);

            return DownloadAsync(downloadOptions, cancellationToken);
        }

        public async Task DownloadAsync(DownloadDedupManifestArtifactOptions downloadOptions, CancellationToken cancellationToken)
        {
            this.Trace_X_TFS_SessionId();
            Uri proxyUri = null;
            var artifactNameAndManifestIds = downloadOptions.ArtifactNameAndManifestIds;

            // ManifestId and ArtifactNameAndManifestIds can both be null
            if ((artifactNameAndManifestIds == null || artifactNameAndManifestIds.Count == 0) && downloadOptions.ManifestId == null)
            {
                throw new ArgumentNullException("No valid manifest ID provided.");
            }

            // ManifestId and ArtifactNameAndManifestIds cannot both exist - this condition should never happen
            if (artifactNameAndManifestIds != null && downloadOptions.ManifestId != null)
            {
                throw new ArgumentException("Options cannot contain both multi download and single download parameters.");
            }

            if (downloadOptions.ManifestId != null && artifactNameAndManifestIds == null)
            {
                artifactNameAndManifestIds = new Dictionary<string, DedupIdentifier>
                {
                    { string.Empty, downloadOptions.ManifestId }
                };
            }
            var downloadStatistics = new DedupDownloadStatistics();

            foreach (KeyValuePair<string, DedupIdentifier> artifactNameAndManifestId in artifactNameAndManifestIds)
            {
                if (!string.IsNullOrWhiteSpace(artifactNameAndManifestId.Key))
                {
                    tracer.Info($"Start downloading artifact - {artifactNameAndManifestId.Key}");
                }

                var targetDirectory = downloadOptions.ManifestId == null ? Path.Combine(downloadOptions.TargetDirectory, artifactNameAndManifestId.Key) : downloadOptions.TargetDirectory;

                DownloadDedupManifestArtifactOptions options = DownloadDedupManifestArtifactOptions.CreateWithManifestId(
                    artifactNameAndManifestId.Value,
                    targetDirectory,
                    proxyUri: proxyUri,
                    minimatchPatterns: downloadOptions.MinimatchPatterns,
                    artifactNameAndManifestId.Key,
                    minimatchFilterWithArtifactName: downloadOptions.MinimatchFilterWithArtifactName);

                HashSet<string> excludedPaths = CreateEmptyExcludedPaths();

                var currentStatistics = await DownloadSingleManifestAsync(options, downloadManifestReferences: true, excludedPaths, cancellationToken);
                downloadStatistics.ConcatenateStatistics(currentStatistics);
            }

            // Send Download Telemetry
            DedupDownloadTelemetryRecord downloadRecord = ClientTelemetry.CreateRecord<DedupDownloadTelemetryRecord>((level, uri, prefix) =>
                new DedupDownloadTelemetryRecord(level, uri, prefix, nameof(DownloadAsync), downloadStatistics));
            ClientTelemetry.SendRecord(downloadRecord);

            tracer.Info("Download completed.");

            if (artifactNameAndManifestIds.Count > 1)
            {
                string summary = this.client.DownloadStatistics.AsString();
                tracer.Info("\nAll the artifacts were downloaded successfully.\n\nDownload Summary:\n" + summary);
            }
        }

        public Task DownloadAsyncWithManifestPath(string fullManifestPath, string targetDirectory, Uri proxyUri, CancellationToken cancellationToken)
        {
            DownloadDedupManifestArtifactOptions downloadOptions = DownloadDedupManifestArtifactOptions.CreateWithManifestPath(
                fullManifestPath,
                targetDirectory,
                proxyUri: proxyUri);
            return DownloadAsyncWithManifestPath(downloadOptions, cancellationToken);
        }

        public Task DownloadAsyncWithManifestPath(
            DownloadDedupManifestArtifactOptions downloadOptions,
            CancellationToken cancellationToken)
        {
            IEnumerable<Func<string, bool>> minimatcherFuncs = MinimatchHelper.GetMinimatchFuncs(downloadOptions.MinimatchPatterns, tracer);

            HashSet<string> excludedPaths = CreateEmptyExcludedPaths();

            return DownloadAsyncWithManifestPath(downloadOptions, minimatcherFuncs, downloadManifestReferences: true, excludedPaths, cancellationToken);
        }

        private async Task<DedupDownloadStatistics> DownloadAsyncWithManifestPath(
            DownloadDedupManifestArtifactOptions downloadOptions,
            IEnumerable<Func<string, bool>> minimatcherFuncs,
            bool downloadManifestReferences,
            ISet<string> excludedPaths,
            CancellationToken cancellationToken)
        {
            Uri proxyUri = downloadOptions.ProxyUri;
            string fullManifestPath = downloadOptions.AbsoluteManifestPath;
            var manifestStr = this.fileSystem.ReadAllText(fullManifestPath);
            var manifest = JsonSerializer.Deserialize<Manifest>(manifestStr);
            var artifactName = downloadOptions.ArtifactName;

            var targetDirectory = downloadOptions.TargetDirectory;

            FixupEmptyDirectoriesFromBrokenManifest(manifest);

            this.client.ResetDownloadStatistics();

            if (minimatcherFuncs != null && minimatcherFuncs.Count() != 0)
            {
                manifest = this.GetFilteredManifest(artifactName, manifest, minimatcherFuncs, downloadOptions.MinimatchFilterWithArtifactName);
            }

            IEnumerable<ManifestItem> filteredManifestItems = manifest.Items.Where(i => !excludedPaths.Contains(i.Path));

            ulong totalContentBytes = 0;

            foreach (var entry in filteredManifestItems)
            {
                if (entry.Type == ManifestItemType.File)
                {
                    totalContentBytes += entry.Blob.Size;
                }
            }

            var dataportInitResult = await DedupVolumeChunkStore.GetDataPortAsync(targetDirectory, cancellationToken, msg => { tracer.Verbose(msg); }).ConfigureAwait(false);
            var maybeDataport = dataportInitResult.Match(
                d => d,
                errorMsg =>
                {
                    tracer.Info("Could not initialize dataport.");
                    tracer.Verbose(errorMsg);
                    return null;
                }
            );

            var statsCancelationSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var reportTask = Task.Run(async () =>
            {
                while (!statsCancelationSrc.IsCancellationRequested)
                {
                    this.TraceDownloadProgress(totalContentBytes);
                    await Task.Delay(StatsInterval, statsCancelationSrc.Token).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            if (maybeDataport == null)
            {
                var batchItemsBlock = new BatchBlock<ManifestItem>(
                    batchSize: 1000,
                    new GroupingDataflowBlockOptions()
                    {
                        CancellationToken = cancellationToken,
                    });

                var fetchUrisBlock = NonSwallowingTransformManyBlock.Create<IEnumerable<ManifestItem>, (ManifestItem, GetDedupAsyncFunc)>(
                    async itemPage =>
                    {
                        var ids = new HashSet<DedupIdentifier>(itemPage.Select(i => DedupIdentifier.Create(i.Blob.Id)));
                        Dictionary<DedupIdentifier, GetDedupAsyncFunc> getters = await this.client.GetDedupGettersAsync(ids, proxyUri: null, edgeCache: edgeCache, cancellationToken: cancellationToken).ConfigureAwait(false);
                        return itemPage.Select(i => (i, getters[DedupIdentifier.Create(i.Blob.Id)]));
                    },
                    new ExecutionDataflowBlockOptions()
                    {
                        BoundedCapacity = 4 * client.MaxParallelismCount,
                        MaxDegreeOfParallelism = client.MaxParallelismCount,
                        CancellationToken = cancellationToken
                    });

                var downloadBlock = NonSwallowingActionBlock.Create<(ManifestItem entry, GetDedupAsyncFunc getter)>(async item =>
                    {
                        var path = Path.Combine(targetDirectory, item.entry.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        this.CreateDirectoryIfDoesntExist(path, fullPathIsFile: true);
                        await this.client.DownloadToFileAsync(
                            maybeDataport,
                            DedupIdentifier.Create(item.entry.Blob.Id),
                            path,
                            item.entry.Blob.Size,
                            item.getter,
                            proxyUri,
                            edgeCache,
                            cancellationToken).ConfigureAwait(false);
                    },
                    new ExecutionDataflowBlockOptions()
                    {
                        BoundedCapacity = 4 * client.MaxParallelismCount,
                        MaxDegreeOfParallelism = client.MaxParallelismCount,
                        CancellationToken = cancellationToken,
                    });

                batchItemsBlock.LinkTo(fetchUrisBlock, new DataflowLinkOptions() { PropagateCompletion = true });
                fetchUrisBlock.LinkTo(downloadBlock, new DataflowLinkOptions() { PropagateCompletion = true });

                var manifestItemsWithoutEmptyDirectories = manifest.Items.Where(i => i.Type != ManifestItemType.EmptyDirectory);
                try
                {
                    await batchItemsBlock.SendAllAndCompleteAsync(manifestItemsWithoutEmptyDirectories, downloadBlock, cancellationToken).ConfigureAwait(false);
                }
                // FEEDBACK TICKET: #1501914, https://dev.azure.com/mseng/AzureDevOps/_workitems/edit/1501914
                // This exception was thrown and the logs were not outputting the message effectively enough, placed here to get more logging data in the cases that a customer is receiving this error back
                // feel free to remove this if there isn't evidence of this occurring
                catch (VssServiceResponseException e)
                {
                    this.tracer.Info($"Response exception thrown: {e.ToString()} " +
                        $"\n Status code: {e.HttpStatusCode} " +
                        $"\n Inner exception: {e.InnerException?.ToString()} " +
                        $"\n Source: {e.Source}");
                    throw;
                }
            }
            else
            {
                var dataport = maybeDataport;
                var emptyFiles = filteredManifestItems.Where(mapping =>
                    (mapping.Type == ManifestItemType.File)
                    && (mapping.Blob.Size == 0));

                Task emptyFileTask = Task.Run(() =>
                {
                    byte[] emptyBytes = new byte[0];
                    foreach (var emptyFile in emptyFiles)
                    {
                        var path = Path.Combine(targetDirectory, emptyFile.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        try
                        {
                            this.fileSystem.WriteAllBytes(path, emptyBytes);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            this.CreateDirectoryIfDoesntExist(path, fullPathIsFile: true);
                            this.fileSystem.WriteAllBytes(path, emptyBytes);
                        }
                    }
                });

                var nonZeroFiles = filteredManifestItems.Where(entry =>
                    entry.Type == ManifestItemType.File
                    && entry.Blob.Size != 0);

                this.tracer.Info($"Walking nodes to enumerate all chunks.");

                var fileNodes = new ConcurrentDictionary<DedupIdentifier, DedupNode>();
                {
                    var nodesToFill = new HashSet<DedupIdentifier>(
                        nonZeroFiles
                            .Where(entry => entry.Blob.Id.EndsWith("2"))
                            .Select(entry => DedupIdentifier.Create(entry.Blob.Id)));
                    var nodeGetters = await this.client.Client.GetDedupGettersAsync(nodesToFill, proxyUri, edgeCache, cancellationToken).ConfigureAwait(false);

                    var fillNodeQueue = NonSwallowingActionBlock.Create<KeyValuePair<DedupIdentifier, GetDedupAsyncFunc>>(
                        async nodeGetter =>
                        {
                            DedupNode node;
                            using (DedupCompressedBuffer nodeBuffer = await nodeGetter.Value(cancellationToken).ConfigureAwait(false))
                            {
                                node = DedupNode.Deserialize(nodeBuffer.Uncompressed);
                                node = await client.GetFilledNodesAsync(node, proxyUri, edgeCache, cancellationToken).ConfigureAwait(false);
                                fileNodes[nodeGetter.Key] = node;
                            }
                        },
                        new ExecutionDataflowBlockOptions()
                        {
                            MaxDegreeOfParallelism = client.MaxParallelismCount,
                            CancellationToken = cancellationToken,
                        });

                    await fillNodeQueue.PostAllToUnboundedAndCompleteAsync(nodeGetters, cancellationToken).ConfigureAwait(false);
                }

                foreach (var chunkFile in nonZeroFiles.Where(entry => entry.Blob.Id.EndsWith("1")))
                {
                    var chunkNode = new DedupNode(new ChunkInfo(0, (uint)chunkFile.Blob.Size, DedupIdentifier.Create(chunkFile.Blob.Id).AlgorithmResult));
                    fileNodes.TryAdd(new ChunkDedupIdentifier(chunkNode.Hash), chunkNode);
                }

                var allChunks = fileNodes.Values
                    .SelectMany(n => n.EnumerateChunkLeafsInOrder())
                    .Distinct()
                    .ToList();
                tracer.Info($"Ensuring {allChunks.Count} chunks are in the chunk store.");
                await client.EnsureChunksAreLocalAsync(dataport, allChunks, proxyUri, edgeCache, cancellationToken).ConfigureAwait(false);

                this.tracer.Info($"Creating dedup reparse points for {manifest.Items.Count} files.");
                var queue = NonSwallowingActionBlock.Create<IReadOnlyList<ManifestItem>>(
                    async outerPage =>
                    {
                        var innerPages = new Queue<IReadOnlyList<ManifestItem>>();
                        innerPages.Enqueue(outerPage);
                        while (innerPages.Any())
                        {
                            var page = innerPages.Dequeue();

                            var streams = new DedupStream[page.Count];
                            var entries = new DedupStreamEntry[page.Sum(entry => fileNodes[DedupIdentifier.Create(entry.Blob.Id)].EnumerateChunkLeafsInOrder().Count())];
                            int chunkIndex = 0;
                            for (int i = 0; i < page.Count; i++)
                            {
                                ManifestItem entry = page[i];
                                DedupNode node = fileNodes[DedupIdentifier.Create(entry.Blob.Id)];

                                uint chunkCount = 0;
                                ulong chunkOffset = 0;
                                foreach (var chunk in node.EnumerateChunkLeafsInOrder())
                                {
                                    entries[chunkIndex] = new DedupStreamEntry()
                                    {
                                        Hash = new DedupHash() { Hash = chunk.Hash },
                                        LogicalSize = (uint)chunk.TransitiveContentBytes,
                                        Offset = chunkOffset,
                                    };

                                    chunkOffset += chunk.TransitiveContentBytes;
                                    chunkIndex++;
                                    chunkCount++;
                                }

                                var path = Path.Combine(
                                    targetDirectory,
                                    entry.Path
                                         // convert manifest's forward slashes to NTFS's back slashes
                                         .Replace('/', '\\')
                                         .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                                streams[i] = new DedupStream()
                                {
                                    ChunkCount = chunkCount,
                                    Length = entry.Blob.Size,
                                    Offset = 0,
                                    Path = path.Substring(2),
                                };
                            }

                            Guid requestId;
                            try
                            {
                                dataport.CommitStreams((uint)streams.Length, streams, (uint)entries.Length, entries, out requestId);
                            }
                            catch (COMException ce) when (ce.ErrorCode == unchecked((int)0x8056536F))
                            {
                                // DDP_E_DATAPORT_TOO_MANY_REQUESTS             = unchecked((int)0x8056536F);
                                // cannot accept request due to size limit exceeded

                                // This can happen when the pages have lots of chunks, so
                                // we'll split the request into two smaller ones and retry those individually.

                                innerPages.Enqueue(page.Take(page.Count / 2).ToList());
                                innerPages.Enqueue(page.Skip(page.Count / 2).ToList());
                                continue;
                            }

                            var result = await dataport.GetResultAsync(requestId).ConfigureAwait(false);
                            if (result.BatchResult != 0 || result.ItemResults.Any(r => r != 0))
                            {
                                throw new InvalidOperationException(string.Format("CommitStream failed 0x{0:x} 0x{1:x}",
                                    result.BatchResult,
                                    result.ItemResults.First(r => r != 0)));
                            }
                        }
                    },
                    new ExecutionDataflowBlockOptions()
                    {
                        MaxDegreeOfParallelism = client.MaxParallelismCount,
                        CancellationToken = cancellationToken,
                    });

                await queue.PostAllToUnboundedAndCompleteAsync(nonZeroFiles.GetPages(100), cancellationToken).ConfigureAwait(false);
            }

            foreach (var entry in filteredManifestItems)
            {
                if (entry.Type == ManifestItemType.EmptyDirectory)
                {
                    excludedPaths.Add(entry.Path);
                    var path = Path.Combine(targetDirectory, entry.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    this.CreateDirectoryIfDoesntExist(path, fullPathIsFile: false);
                }
            }

            this.TraceDownloadProgress(totalContentBytes);
            statsCancelationSrc.Cancel();

            DedupDownloadStatistics currentStatistics = this.client.DownloadStatistics;
            tracer.Info($"{Environment.NewLine}Download statistics:{Environment.NewLine}" + currentStatistics.AsString());

            if (downloadManifestReferences)
            {
                foreach (ManifestReference manifestReference in manifest.ManifestReferences)
                {
                    var refOptions = DownloadDedupManifestArtifactOptions.CreateWithManifestId(
                        manifestReference.ManifestId,
                        downloadOptions.TargetDirectory,
                        downloadOptions.ProxyUri,
                        downloadOptions.MinimatchPatterns,
                        artifactName,
                        minimatchFilterWithArtifactName: true);
                    DedupDownloadStatistics referenceDownloadSatistics = await DownloadSingleManifestAsync(
                        refOptions,
                        downloadManifestReferences: false,
                        excludedPaths,
                        cancellationToken);
                    currentStatistics.ConcatenateStatistics(referenceDownloadSatistics);
                }
            }

            return currentStatistics;
        }

        public Task DownloadFileToPathAsync(
            DedupIdentifier dedupId,
            string fullFileOutputPath,
            Uri proxyUri,
            CancellationToken cancellationToken)
        {
            this.CreateDirectoryIfDoesntExist(fullFileOutputPath, fullPathIsFile: true);
            return this.client.DownloadToFileAsync(dedupId, fullFileOutputPath, dedupFetcher: null, proxyUri: proxyUri, edgeCache: edgeCache, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Download a single manifest
        /// </summary>
        /// <param name="downloadOptions">Download options of the artifact</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="downloadManifestReferences">Only download manifest references when it's true - when it's on the first level manifest</param>
        /// <param name="paths">A list of paths that are downloaded and should be excluded when downloading the next manifest</param>
        private async Task<DedupDownloadStatistics> DownloadSingleManifestAsync(
            DownloadDedupManifestArtifactOptions downloadOptions,
            bool downloadManifestReferences,
            ISet<string> excludedPaths,
            CancellationToken cancellationToken)
        {
            var manifestPath = Path.Combine(Path.GetTempPath(), $"{nameof(DedupManifestArtifactClient)}.{Path.GetRandomFileName()}.manifest");
            IEnumerable<Func<string, bool>> minimatcherFuncs = MinimatchHelper.GetMinimatchFuncs(downloadOptions.MinimatchPatterns, tracer);

            try
            {
                await this.DownloadFileToPathAsync(downloadOptions.ManifestId, manifestPath, downloadOptions.ProxyUri, cancellationToken).ConfigureAwait(false);
                downloadOptions.SetAbsoluteManifestPathAndRemoveManifestId(manifestPath);
                return await this.DownloadAsyncWithManifestPath(downloadOptions, minimatcherFuncs, downloadManifestReferences, excludedPaths, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (fileSystem.FileExists(manifestPath))
                {
                    try
                    {
                        fileSystem.DeleteFile(manifestPath);
                    }
                    catch
                    {
                        tracer.Info("Failed to delete manifest");
                    }
                }
            }
        }

        private Manifest GetFilteredManifest(string artifactName, Manifest manifest, IEnumerable<Func<string, bool>> minimatchFuncs, bool minimatchFilterWithArtifactName)
        {
            var filteredManifestItems = new HashSet<ManifestItem>();
            foreach (ManifestItem item in manifest.Items)
            {
                // trim the leading slash from the item paths if it's single download and add artifact name as part of the target path if it's multidownload
                var path = (string.IsNullOrWhiteSpace(artifactName) || !minimatchFilterWithArtifactName) ? item.Path.TrimStart('/') : artifactName + item.Path;

                if (minimatchFuncs.Any(match => match(path)))
                {
                    filteredManifestItems.Add(item);
                }
            }
            this.tracer.Info($"Filtered {filteredManifestItems.Count} files from the Minimatch filters supplied.");

            return new Manifest(filteredManifestItems.ToList());
        }

        private void CreateDirectoryIfDoesntExist(string fullPath, bool fullPathIsFile)
        {
            string directoryPath = fullPathIsFile ? Path.GetDirectoryName(fullPath) : fullPath;
            if (!this.fileSystem.DirectoryExists(directoryPath))
            {
                this.fileSystem.CreateDirectory(directoryPath);
            }
        }

        private void GenerateManifest(IEnumerable<FileBlobDescriptor> hashes, string manifestFilePath, CancellationToken cancellationToken)
        {
            List<ManifestItem> items = new List<ManifestItem>();

            foreach (var hash in hashes)
            {
                ManifestItemType manifestItemType;
                string path;
                if (hash.AbsolutePath.EndsWith(FileBlobDescriptorConstants.EmptyDirectoryEndingPattern))
                {
                    manifestItemType = ManifestItemType.EmptyDirectory;
                    string pathWithEndingPattern = Locator.Parse(hash.RelativePath).Value;
                    path = pathWithEndingPattern.Remove(pathWithEndingPattern.Length - (FileBlobDescriptorConstants.EmptyDirectoryEndingPattern.Length));
                }
                else
                {
                    manifestItemType = ManifestItemType.File;
                    path = Locator.Parse(hash.RelativePath).Value;
                }

                var item = new ManifestItem(
                    path: path,
                    blob: new DedupInfo(
                    id: hash.BlobIdentifier.ValueString,
                    size: (ulong)hash.FileSize.Value),
                    type: manifestItemType
                );

                items.Add(item);
            }

            items.Sort((i1, i2) => StringComparer.Ordinal.Compare(i1.Path, i2.Path));

            var content = Content.Common.JsonSerializer.Serialize(new Manifest(items));
            string fullManifestFilePath = Path.GetFullPath(manifestFilePath);
            this.CreateDirectoryIfDoesntExist(fullManifestFilePath, fullPathIsFile: true);
            this.fileSystem.WriteAllText(manifestFilePath, content);
        }

        private void TraceDownloadProgress(ulong totalContentBytes)
        {
            int percentageDownloaded;
            if (totalContentBytes > 0)
            {
                percentageDownloaded = (int)Math.Round((((double)this.client.DownloadStatistics.TotalContentBytes) / totalContentBytes) * 100);
            }
            else
            {
                percentageDownloaded = 100;
            }
            tracer.Info($"Downloaded {NumberConversionHelper.ConvertBytesToMegabytes(this.client.DownloadStatistics.TotalContentBytes):N1} MB out of {NumberConversionHelper.ConvertBytesToMegabytes((long)totalContentBytes):N1} MB ({percentageDownloaded}%).");
        }

        //Below function does not fix the manifest, it is for backward compatibility only.
        private void FixupEmptyDirectoriesFromBrokenManifest(Manifest m)
        {
            for (int i = 0; i < m.Items.Count; i++)
            {
                var item = m.Items[i];
                if (item.Type == ManifestItemType.File && Path.GetFileName(item.Path).Equals(".", StringComparison.Ordinal) && item.Blob.Size == 0)
                {
                    m.Items[i] = new ManifestItem(Path.GetDirectoryName(item.Path), blob: null, ManifestItemType.EmptyDirectory);
                }
            }
        }

        private HashSet<string> CreateEmptyExcludedPaths()
        {
            HashSet<string> excludedPaths;
            if (isWindows)
            {
                excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                excludedPaths = new HashSet<string>();
            }
            return excludedPaths;
        }

        private void Trace_X_TFS_SessionId()
        {
            this.tracer.Info($"{nameof(DedupManifestArtifactClient)} will correlate http requests with X-TFS-Session {TelemetryRecord.Current_X_TFS_Session.ToString()}");
        }
    }

    public sealed class PublishResult
    {
        public readonly DedupIdentifier ManifestId;

        public readonly DedupIdentifier RootId;

        public readonly IEnumerable<string> ProofNodes;

        public readonly long FileCount;

        public readonly long ContentSize;

        public PublishResult(DedupIdentifier manifestId, DedupIdentifier root, IEnumerable<string> proofNodes, long fileCount, long contentSize)
        {
            this.ManifestId = manifestId;
            this.RootId = root;
            this.ProofNodes = proofNodes;
            this.FileCount = fileCount;
            this.ContentSize = contentSize;
        }
    }

    // BuildDropManager was the old name for pipeline artifacts, left here for compatibility, we should eventually deprecate this after a while
    [CLSCompliant(false)]
    public class BuildDropManager : DedupManifestArtifactClient, IBuildDropManager
    {
        public BuildDropManager(IDedupStoreClientWithDataport client, IAppTraceSource tracer) : base(client, tracer)
        {
        }

        internal BuildDropManager(IDedupStoreClientWithDataport client, IAppTraceSource tracer, IFileSystem fileSystem) : base(client, tracer, fileSystem)
        {
        }
    }
}
