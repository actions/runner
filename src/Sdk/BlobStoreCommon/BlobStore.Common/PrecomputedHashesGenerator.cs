using GitHub.Services.BlobStore.Common;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.ArtifactServices.App.Shared
{
    /// <summary>
    /// Writes new hashes files and reads existing ones.
    /// </summary>
    /// <remarks>
    /// Directory tree traversal when writing a new hashes file:
    /// The hash generator iterates through a directory tree recursively (depth-first pre-order) via Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories) which uses FindFirstFile/FindNextFile internally.
    /// Paths are then grouped by their parent directory which is likely redundant.
    /// Paths are then divided into pages.
    /// Each page may contain files from multiple directories.
    /// 
    /// Active parallelism configuration:
    /// The number of hash operations which can execute in parallel is determined by the host's processor count via a SemaphoreSlim wrapping the contents of GetFileBlobDescriptorAsync.
    /// 
    /// Vestigial parallelism configuration:
    /// MaxPageSize determines the number of files sent at a time to a DataFlow block which performs at most 16 hash operations in parallel.
    /// MaxParallelDirectoryPublish determines the number of pages being processed in parallel by another Dataflow block. This acts as a multiplier on the parallelism of at most 16 hashing operations per page.
    /// The parallelism intended by these mechanisms is overridden further down the call tree by the SemaphoreSlim described above which bounds parallelism to the host's processor count.
    /// </remarks>
    [CLSCompliant(false)]
    public class PrecomputedHashesGenerator
    {
        private const int DefaultMaxParallelDirectoryHashCount = 128;

        private const int DefaultMaxPageSize = 100;

        private const bool DefaultLowercasePaths = false;

        /// <summary>
        /// Prevents us from needless context switching when hashing
        /// </summary>
        private static readonly SemaphoreSlim MaxParallelComputeHash = new SemaphoreSlim(Environment.ProcessorCount);

        private readonly IAppTraceSource tracer;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Construct a hash generator with default settings
        /// </summary>
        public PrecomputedHashesGenerator(IAppTraceSource tracer) :
            this(
                tracer, 
                FileSystem.Instance)
        {
        }

        public PrecomputedHashesGenerator(
            IAppTraceSource tracer, 
            IFileSystem fileSystem) :
            this(
                tracer,
                fileSystem,
                DefaultMaxPageSize,
                DefaultMaxParallelDirectoryHashCount,
                DefaultLowercasePaths)
        { }

        /// <summary>
        /// Construct a hash generator with overrides for vestigial parallelism settings and filename case handling
        /// </summary>
        public PrecomputedHashesGenerator(
            IAppTraceSource tracer,
            IFileSystem fileSystem,
            int maxPageSize, 
            int maxParallelDirectoryHash, 
            bool lowercasePaths)
        {
            if (tracer == null)
            {
                throw new ArgumentNullException(nameof(tracer));
            }

            if (maxPageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPageSize), "Expected a positive pageSize");
            }

            if (maxParallelDirectoryHash < 1 && maxParallelDirectoryHash != -1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxParallelDirectoryHash), "Expected a positive value or -1 to denote maximum parallelism");
            }

            this.MaxParallelDirectoryPublish = maxParallelDirectoryHash;
            this.MaxPageSize = maxPageSize;
            this.LowercasePaths = lowercasePaths;

            this.tracer = tracer;
            ArgumentUtility.CheckForNull(fileSystem, nameof(fileSystem));
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// See remarks on this class.
        /// Defaults to 100.
        /// Drop overrides to 5000.
        /// </summary>
        internal int MaxPageSize { get; }

        /// <summary>
        /// See remarks on this class.
        /// Defaults to 128.
        /// Drop overrides to 40.
        /// </summary>
        internal int MaxParallelDirectoryPublish { get; }

        /// <summary>
        /// True to change the case of the relative path to lowercase.
        /// False to leave path case unchanged.
        /// Defaults to false.
        /// </summary>
        public bool LowercasePaths { get; }

        /// <summary>
        /// Read a hashes file and deserialize it to descriptor objects.
        /// This is used by publish operations in DropServiceClient, Drop.App, and Symbol.App.
        /// </summary>
        /// <param name="preComputedHashesFile">Hashes file to read</param>
        /// <param name="directory">Path provided as the rootDirectory of each FileBlobDescriptor</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static List<FileBlobDescriptor> LoadPrecomputedHashes(string preComputedHashesFile, string directory, bool lowercasePaths = false)
        {
            directory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            List<FileBlobDescriptor> preComputedHashes = null;

            using (
                Stream stream = FileStreamUtils.OpenFileStreamForAsync(
                    preComputedHashesFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    preComputedHashes = new List<FileBlobDescriptor>();
                    string line;
                    while (null != (line = reader.ReadLine()))
                    {
                        preComputedHashes.Add(FileBlobDescriptor.Deserialize(directory, line));
                    }
                    if (lowercasePaths)
                    {
                        foreach (var hash in preComputedHashes)
                        {
                            hash.RelativePath = hash.RelativePath.ToLowerInvariant();
                        }
                    }
                }
            }

            return preComputedHashes;
        }

        /// <summary>
        /// Calculate hashes for a directory's contents and write them to a precomputed hashes file.
        /// This is used by hash operations in DropServiceClient, Drop.App, and Symbol.App.
        /// </summary>
        /// <param name="precomputedHashesFileName">Hashes file to write.</param>
        /// <param name="directory">Directory of content to hash.</param>
        /// <param name="fileListFileName">The file list name.</param>
        /// <param name="chunkDedup">If set to <c>true</c> [uses chunk dedup].</param>
        /// <param name="includeEmptyDirectories">If set to <c>true</c> [precomputes hashes for empty directories as well].</param>
        /// <param name="artifactPublishOptions">Options for publishing a drop.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>A collection of FileBlobDescriptors.</returns>
        public async Task<List<FileBlobDescriptor>> GeneratePrecomputedHashesAsync(string precomputedHashesFileName, string directory, 
            string fileListFileName, bool chunkDedup, bool includeEmptyDirectories, ArtifactPublishOptions artifactPublishOptions, CancellationToken cancellationToken)
        {
            directory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var files = new List<FileBlobDescriptor>();

            using (StreamWriter hashFileStream = new StreamWriter(precomputedHashesFileName))
            {
                using (var fileBlobIds = new BlockingCollection<FileBlobDescriptor>())
                {
                    var loggerTask = Task.Run(
                        async () =>
                        {
                            foreach (var fileBlobId in fileBlobIds.GetConsumingEnumerable())
                            {
                                files.Add(fileBlobId);
                                await hashFileStream.WriteLineAsync(fileBlobId.Serialize()).ConfigureAwait(false);
                            }
                        }, cancellationToken);

                    List<FileInfo> filePathList = null;

                    if (!string.IsNullOrEmpty(fileListFileName))
                    {
                        filePathList = await ListOfFiles.LoadFileListAsync(fileListFileName, directory).ConfigureAwait(false);
                    }

                    // drop.exe publish codepath
                    long fileCount = await this.PaginateAndProcessFiles(
                        directory,
                        filePathList,
                        chunkDedup,
                        includeEmptyDirectories,
                        artifactPublishOptions,
                        cancellationToken,
                        hashCompleteCallback: fileBlobId => fileBlobIds.Add(fileBlobId, cancellationToken)).ConfigureAwait(false);

                    fileBlobIds.CompleteAdding();
                    await loggerTask.ConfigureAwait(false);
                }
            }

            return files;
        }

        /// <summary>
        /// Given a set of paths, compute the hashes of each file and pass the resulting
        /// FileBlobDescriptor for each file to a callback Action.
        /// </summary>
        /// <param name="sourceDirectory">name of directory to process.</param>
        /// <param name="filePaths">The file paths.</param>
        /// <param name="chunkDedup">if set to <c>true</c> [chunk dedup].</param>
        /// <param name="includeEmptyDirectories">if set to <c>true</c> [include empty directories].</param>
        /// <param name="artifactPublishOptions">Options for publishing a drop.</param>
        /// <param name="cancellationToken">CancellationToken to set if asynchronous action should be canceled.</param>
        /// <param name="hashCompleteCallback">Callback Action to perform using the resulting FileBlobDescriptor computed for each file.</param>
        /// <remarks>This method is used by consumers outside VSTS ArtifactServices including vPack.</remarks>
        /// <returns>File count.</returns>
        public Task<long> PaginateAndProcessFiles(
            string sourceDirectory,
            IEnumerable<FileInfo> filePaths,
            bool chunkDedup,
            bool includeEmptyDirectories,
            ArtifactPublishOptions artifactPublishOptions,
            CancellationToken cancellationToken,
            Action<FileBlobDescriptor> hashCompleteCallback)
        {
            IEnumerable<IEnumerable<PageItem>> pages = filePaths != null ?
                this.GetPagesFromPaths(filePaths) :
                this.GetSegmentedPagesFromSourceDirectory(sourceDirectory, includeEmptyDirectories, artifactPublishOptions);

            return this.PaginateAndProcessFiles(sourceDirectory, chunkDedup, pages, cancellationToken, hashCompleteCallback);
        }

        /// <summary>
        /// Given a directory name, compute the hashes of the files (recursively) and pass the resulting
        /// FileBlobDescriptor for each file to a callback Action.
        /// </summary>
        /// <param name="sourceDirectory">The name of directory to process.</param>
        /// <param name="chunkDedup">If set to <c>true</c> [chunk dedup].</param>
        /// <param name="includeEmptyDirectories">If set to <c>true</c> [include empty directories].</param>
        /// <param name="artifactPublishOptions">Options for publishing a drop.</param>
        /// <param name="cancellationToken">CancellationToken to set if asynchronous action should be canceled.</param>
        /// <param name="hashCompleteCallback">Callback Action to perform using the resulting FileBlobDescriptor computed for each file.</param>
        /// <remarks>This method is used by consumers outside VSTS ArtifactServices including vPack.</remarks>
        public Task<long> PaginateAndProcessFiles(
            string sourceDirectory,
            bool chunkDedup,
            bool includeEmptyDirectories,
            ArtifactPublishOptions artifactPublishOptions,
            CancellationToken cancellationToken,
            Action<FileBlobDescriptor> hashCompleteCallback)
        {
            IEnumerable<IEnumerable<PageItem>> pages = this.GetSegmentedPagesFromSourceDirectory(sourceDirectory, includeEmptyDirectories, artifactPublishOptions);
            return this.PaginateAndProcessFiles(sourceDirectory, chunkDedup, pages, cancellationToken, hashCompleteCallback);
        }

        /// <summary>
        /// Given Pages of PageItems to process, compute the hashes of the files at the items' path and pass the resulting
        /// FileBlobDescriptor for each file to a callback Action.
        /// </summary>
        /// <param name="sourceDirectory">name of directory to process.</param>
        /// <param name="chunkDedup">if set to <c>true</c> [chunk dedup].</param>
        /// <param name="pages">The pages.</param>
        /// <param name="cancellationToken">CancellationToken to set if asynchronous action should be canceled.</param>
        /// <param name="hashCompleteCallback">Callback Action to perform using the resulting FileBlobDescriptor computed for each file.</param>
        /// <remarks>This method is used by consumers outside VSTS ArtifactServices including vPack.</remarks>
        private async Task<long> PaginateAndProcessFiles(
            string sourceDirectory,
            bool chunkDedup,
            IEnumerable<IEnumerable<PageItem>> pages,
            CancellationToken cancellationToken,
            Action<FileBlobDescriptor> hashCompleteCallback)
        {
            long processedTotal = 0;
            int page = 0;
            int totalPages = pages.Count();
            int totalFiles = pages.Sum(p => p.Count());
            this.tracer.Info($"{totalFiles} files to be processed in {totalPages} groups.");

            var taskQueue = NonSwallowingActionBlock.Create<IEnumerable<PageItem>>(
                async pageOfFiles =>
                    {
                        try
                        {
                            int currentPage = Interlocked.Increment(ref page);
                            long processedCount =
                                await
                                this.PaginateAndProcessFilesHelperAsync(
                                    sourceDirectory,
                                    chunkDedup,
                                    pageOfFiles,
                                    hashCompleteCallback,
                                    currentPage,
                                    totalPages,
                                    cancellationToken).ConfigureAwait(false);
                            Interlocked.Add(ref processedTotal, processedCount);
                            this.tracer.Info($"{processedTotal} out of {totalFiles} files processed (Group: {currentPage}/{totalPages})");
                        }
                        catch (Exception ex)
                        {
                            this.tracer.Error($"Failed to hash a page of files. Exception listed below...");
                            this.tracer.Error(ex);
                            throw;
                        }
                    },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = this.MaxParallelDirectoryPublish,
                    CancellationToken = cancellationToken,
                });

            await taskQueue.PostAllToUnboundedAndCompleteAsync(pages, cancellationToken).ConfigureAwait(false);

            this.tracer.Info($"Processed {processedTotal} files from {sourceDirectory} successfully.");
            return processedTotal;
        }

        internal IEnumerable<IEnumerable<PageItem>> GetPagesFromPaths(IEnumerable<FileInfo> paths)
        {
            return paths
               .Select(path => new PageItem(path.FullName, PageItemType.File))
               .OrderBy(path => path.Path)
               .GetPages(this.MaxPageSize);
        }

        /// <summary>
        /// Fetches the segmented PageItem's from the source directory.
        /// </summary>
        /// <param name="sourceDirectory">The name of directory to process.</param>
        /// <param name="includeEmptyDirectories">If set to <c>true</c> [include empty directories].</param>
        /// <param name="artifactPublishOptions">Publish options for the drop.</param>
        /// <returns>A collection of a collection of PageItems.</returns>
        internal IEnumerable<IEnumerable<PageItem>> GetSegmentedPagesFromSourceDirectory(string sourceDirectory, bool includeEmptyDirectories, ArtifactPublishOptions artifactPublishOptions)
        {
            var defaultStringComparer = Helpers.FileSystemStringComparer(Environment.OSVersion);

            var emptyDirectories = new HashSet<string>(defaultStringComparer);
            
            var ticktock = new Stopwatch();
            ticktock.Start();

            var ignoreFiles = new HashSet<string>(defaultStringComparer);

            if (artifactPublishOptions.HonorIgnoreOptions)
            {
                var globFactory = new GlobFactory(this.fileSystem, this.tracer, globEmptyDirectories: includeEmptyDirectories);
                ignoreFiles = globFactory.PerformGlobbing(sourceDirectory).ToHashSet(x => x, defaultStringComparer);
            }

            ticktock.Stop();

            this.tracer.Verbose("Globbing completed in: {0}", ticktock.Elapsed);

            ticktock.Restart();

#if NET_STANDARD
            var files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories).ToHashSet(x => x);
#else
            var files = this.fileSystem.EnumerateFiles(sourceDirectory, true).ToHashSet(x => x, defaultStringComparer);
#endif

            // Generate collection of empty directories.
            if (includeEmptyDirectories)
            {
#if NET_STANDARD
                var directories = Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories).ToHashSet(x => x, defaultStringComparer);
#else
                var directories = this.fileSystem.EnumerateDirectories(sourceDirectory, recursiveSearch: true).ToHashSet(x => x, defaultStringComparer);
#endif
                var nonEmptyDirsFull = new HashSet<string>();

                // Top level directories.
                foreach (var path in files)
                {
                    nonEmptyDirsFull.Add(Path.GetDirectoryName(path));
                }

                // Inner directories.
                foreach (var directory in directories)
                {
                    nonEmptyDirsFull.Add(Path.GetDirectoryName(directory));
                }

                emptyDirectories = directories.Where(d => !nonEmptyDirsFull.Contains(d)).ToHashSet(x => x, defaultStringComparer);
            }

            ticktock.Stop();

            this.tracer.Verbose("All files discovered in: {0}", ticktock.Elapsed);

            ticktock.Restart();
            
            if (artifactPublishOptions.HonorIgnoreOptions && ignoreFiles.Any())
            {
                // Subtract files to be ignored.
                files.ExceptWith(ignoreFiles);
            }

            // Populate file pages.
            var filePages = files
                .Select(f => new PageItem(f, PageItemType.File))
                .GetPages(this.MaxPageSize);

            var pages = filePages.Where(page => page.Count > 0).ToList();

            if (includeEmptyDirectories)
            {
                if (artifactPublishOptions.HonorIgnoreOptions && emptyDirectories.Any())
                {
                    // Subtract empty dirs to be ignored.
                    emptyDirectories.ExceptWith(ignoreFiles);
                }

                // Populate directory pages.
                var emptyDirectoryPages = emptyDirectories
                    .Select(d => new PageItem(d + Path.DirectorySeparatorChar + ".", PageItemType.EmptyDirectory))
                    .GetPages(this.MaxPageSize);

                pages.AddRange(emptyDirectoryPages.Where(emptyDirectoryPage => emptyDirectoryPage.Count > 0));
            }

            ticktock.Stop();

            this.tracer.Verbose("Pages to upload computed in {0}", ticktock.Elapsed);

            return pages;
        }

        private async Task<long> PaginateAndProcessFilesHelperAsync(
            string sourceDirectory,
            bool chunkDedup,
            IEnumerable<PageItem> pathsInDir,
            Action<FileBlobDescriptor> hashCompleteCallback,
            int currentPage,
            int totalPages,
            CancellationToken cancellationToken)
        {
            var fileIds = new ConcurrentBag<FileBlobDescriptor>();
            var taskQueue = NonSwallowingActionBlock.Create<PageItem>(
                async path =>
                    {
                        // drop.exe publish codepath
                        var fileChunkId = await this.GetFileBlobDescriptorAsync(
                            sourceDirectory, 
                            chunkDedup, 
                            path, 
                            cancellationToken).ConfigureAwait(false);
                        fileIds.Add(fileChunkId);

                        if (hashCompleteCallback != null)
                        {
                            hashCompleteCallback(fileChunkId);
                        }
                    },
                new ExecutionDataflowBlockOptions() {
                    MaxDegreeOfParallelism = 16,
                    CancellationToken = cancellationToken,
                });

            Task task = taskQueue.PostAllToUnboundedAndCompleteAsync(pathsInDir, cancellationToken);
            await task.ConfigureAwait(false);

            return fileIds.Count();
        }

        private async Task<FileBlobDescriptor> GetFileBlobDescriptorAsync(string rootDirectory, bool chunkDedup, PageItem pageItem, CancellationToken cancellationToken)
        {
            if (!pageItem.Path.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Path '{pageItem.Path}' does not start with '{rootDirectory}'.", nameof(pageItem));
            }

            await MaxParallelComputeHash.WaitAsync().ConfigureAwait(false);
            try
            {
                string relativePath = pageItem.Path
                    .Substring(rootDirectory.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (this.LowercasePaths)
                {
                    relativePath = relativePath.ToLower();
                }

                // drop.exe publish codepath
                return await FileBlobDescriptor.CalculateAsync(
                    this.fileSystem,
                    rootDirectory,
                    chunkDedup,
                    relativePath,
                    (FileBlobType)Enum.Parse(typeof(FileBlobType), pageItem.Type.ToString(), ignoreCase:true),
                    cancellationToken);
            }
            catch (FileNotFoundException fnex)
            {
                FileAttributes attributes;
                FileLoadException loadException = null;

                try
                {
                    attributes = File.GetAttributes(pageItem.Path);
                    if (attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        loadException = new FileLoadException(BlobStoreCommonResources.SymLinkExceptionMessage(), fnex);
                    }
                }
                catch (Exception)
                {
                    // Intentional empty catch -- if we can't get attributes, we will throw original exception
                }

                if (null == loadException)
                {
                    this.tracer.Error($"Failed to calculate hash for file: {pageItem.Path}");
                    throw;
                }
                else
                {
                    this.tracer.Error($"Failed to calculate hash for the file's reparse point: {pageItem.Path}");
                    throw loadException;
                }
            }
            finally
            {
                MaxParallelComputeHash.Release();
            }
        }
    }

    public struct PageItem
    {
        public readonly string Path;
        public readonly PageItemType Type;

        public PageItem(string path, PageItemType type)
        {
            this.Path = path;
            this.Type = type;
        }
    }

    public enum PageItemType
    {
        File,
        EmptyDirectory,
    }
}
