using GitHub.Services.BlobStore.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class BlobToFileMappingHelper
    {
        /// <summary>
        /// Gets all mappings that don't have existing files,
        /// </summary>
        /// <param name="mappings">Blob to file mappings.</param>
        /// <param name="tracer">Tracer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Returns all mappings with missing files.</returns>
        public static async Task<IEnumerable<BlobToFileMapping>> GetMissingFileMappingsInParallel(
            this IEnumerable<BlobToFileMapping> mappings,
            IAppTraceSource tracer,
            CancellationToken cancellationToken)
        {
            var missingFiles = new List<BlobToFileMapping>();
            var queue = NonSwallowingActionBlock.Create<BlobToFileMapping>(mapping =>
            {
                if (!string.IsNullOrEmpty(mapping.FilePath) && !File.Exists(Path.GetFullPath(mapping.FilePath)))
                {
                    lock (missingFiles)
                    {
                        missingFiles.Add(mapping);
                    }
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            });

            tracer.Info($"Validating existence of all non-empty {nameof(BlobToFileMapping)}.{nameof(BlobToFileMapping.FilePath)}s");
            Stopwatch timer = Stopwatch.StartNew();
            await queue.PostAllToUnboundedAndCompleteAsync(mappings, cancellationToken).ConfigureAwait(false);
            timer.Stop();
            tracer.Info($"Validation of file mappings finished in {timer.ElapsedMilliseconds} ms.");

            return missingFiles;
        }

        /// <summary>
        /// Gets all mappings that have files with inconsistent hashes.
        /// </summary>
        /// <param name="mapping">Blob to file mappings.</param>
        /// <param name="chunkDedup">Is the blob mapping a chunk dedup.</param>
        /// <param name="tracer">Tracer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Returns if the blob mapping hash is the same as the file hash.</returns>
        public static async Task<bool> HashMatchesFileContent(this BlobToFileMapping mapping, 
            bool chunkDedup,
            IAppTraceSource tracer,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(Path.GetFullPath(mapping.FilePath)))
            {
                return false;
            }

            tracer.Verbose($"Validating the expected hash of the file {mapping.BlobId.AlgorithmResultString} matches the actual hash of the file at: {mapping.FilePath}");
            Stopwatch timer = Stopwatch.StartNew();
            string actualHash;
            if (chunkDedup)
            {
                actualHash = (await ChunkerHelper.CreateFromFileAsync(FileSystem.Instance, mapping.FilePath, cancellationToken, false)
                    .ConfigureAwait(false)).Hash.ToHexString();
            }
            else
            {
                using (var stream = FileStreamUtils.OpenFileStreamForAsync(mapping.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                {
                    actualHash = (await VsoHash.CalculateBlobIdentifierWithBlocksAsync(stream).ConfigureAwait(false)).BlobId.AlgorithmResultString;
                }
            }
            string expectedHash = mapping.BlobId.AlgorithmResultString;
            timer.Stop();

            string validationResultMsg = $"Validation of file hash finished {timer.ElapsedMilliseconds} ms. The file path is: {mapping.FilePath}. ";
            if (actualHash != expectedHash)
            {
                tracer.Verbose(validationResultMsg + "The file hash does not match.");
                return false;
            }
            else
            {
                tracer.Verbose(validationResultMsg + "The file hash does match.");
                return true;
            }
        }
    }
}
