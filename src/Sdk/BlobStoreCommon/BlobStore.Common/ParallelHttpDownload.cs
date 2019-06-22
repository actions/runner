using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.Common;

namespace GitHub.Services.BlobStore.Common
{
    public static class ParallelHttpDownload
    {
        public sealed class Configuration
        {
            private const int DefaultSegmentDownloadTimeoutInMinutes = 10;
            private const int DefaultParallelDownloadSegmentSizeInBytes = 8 * 1024 * 1024;
            private const int DefaultMaxParallelSegmentDownloadsPerFile = 16;

            // MaxSegmentDownloadRetries defined here
            private const int DefaultMaxSegmentDownloadRetries = 10;
            private const int DefaultReadSizeInBytes = 64 * 1024;

            public const string DefaultEnvironmentVariablePrefix = "VSO_AS_HTTP_";

            public static Configuration GetParallelSegmentDownloadConfig(string environmentVariablePrefix)
            {
                ArgumentUtility.CheckStringForNullOrEmpty(environmentVariablePrefix, nameof(environmentVariablePrefix));

                return new Configuration(
                    // Ad-hoc test: segmentDownloadTimeout: TimeSpan.FromSeconds(0),
                    segmentDownloadTimeout: TimeSpan.FromMinutes(int.Parse(Environment.GetEnvironmentVariable($"{environmentVariablePrefix}SegmentDownloadTimeoutInMinutes") ?? DefaultSegmentDownloadTimeoutInMinutes.ToString())),
                    segmentSizeInBytes: int.Parse(Environment.GetEnvironmentVariable($"{environmentVariablePrefix}ParallelDownloadSegmentSizeInBytes") ?? DefaultParallelDownloadSegmentSizeInBytes.ToString()),
                    maxParallelSegmentDownloadsPerFile: int.Parse(Environment.GetEnvironmentVariable($"{environmentVariablePrefix}MaxParallelSegmentDownloadsPerFile") ?? DefaultMaxParallelSegmentDownloadsPerFile.ToString()),
                    maxSegmentDownloadRetries: int.Parse(Environment.GetEnvironmentVariable($"{environmentVariablePrefix}MaxSegmentDownloadRetries") ?? DefaultMaxSegmentDownloadRetries.ToString()));
            }

            public static int GetBufferSize(string environmentVariablePrefix)
            {
                environmentVariablePrefix = environmentVariablePrefix ?? DefaultEnvironmentVariablePrefix;

                return int.Parse(Environment.GetEnvironmentVariable($"{environmentVariablePrefix}ReadSizeInBytes") ?? DefaultReadSizeInBytes.ToString());
            }

            public readonly TimeSpan SegmentDownloadTimeout;
            public readonly int SegmentSizeInBytes;
            public readonly int MaxParallelSegmentDownloadsPerFile;
            public readonly int MaxSegmentDownloadRetries;

            public Configuration(TimeSpan segmentDownloadTimeout, int segmentSizeInBytes, int maxParallelSegmentDownloadsPerFile, int maxSegmentDownloadRetries)
            {
                SegmentDownloadTimeout = segmentDownloadTimeout;
                SegmentSizeInBytes = segmentSizeInBytes;
                MaxParallelSegmentDownloadsPerFile = maxParallelSegmentDownloadsPerFile;
                MaxSegmentDownloadRetries = maxSegmentDownloadRetries;
            }
        }

        public static ByteArrayPool CreateBufferPool(string environmentVariablePrefix = null)
        {
            return new ByteArrayPool(Configuration.GetBufferSize(environmentVariablePrefix), maxToKeep: 4 * Environment.ProcessorCount);
        }

        public delegate Task<Stream> StreamSegmentFactory(long offset, CancellationToken cancellationToken);
        public delegate Task<Stream> StreamSegmentFactory2(long offset, long endOffset, CancellationToken cancellationToken);
        public delegate void LogSegmentStart(string destinationPath, long startOffset, long endOffset);
        public delegate void LogSegmentStop(string destinationPath, long startOffset, long endOffset);
        public delegate void LogSegmentFailed(string destinationPath, long startOffset, long endOffset, string message);

        public static Task<long> Download(
            Configuration config,
            Uri uri,
            Stream httpStream,
            long? knownLength,
            string destinationPath,
            FileMode mode,
            CancellationToken cancellationToken,
            LogSegmentStart logSegmentStart,
            LogSegmentStop logSegmentStop,
            LogSegmentFailed logSegmentFailed,
            StreamSegmentFactory segmentFactory,
            Func<IPoolHandle<byte[]>> bufferFactory)
        {
            ArgumentUtility.CheckForNull(config, nameof(config));
            ArgumentUtility.CheckStringForNullOrEmpty(destinationPath, nameof(destinationPath));
            ArgumentUtility.CheckForNull(logSegmentStart, nameof(logSegmentStart));
            ArgumentUtility.CheckForNull(logSegmentStop, nameof(logSegmentStop));
            ArgumentUtility.CheckForNull(logSegmentFailed, nameof(logSegmentFailed));
            ArgumentUtility.CheckForNull(segmentFactory, nameof(segmentFactory));
            ArgumentUtility.CheckForNull(bufferFactory, nameof(bufferFactory));

            return Download(
                config,
                uri,
                httpStream,
                knownLength,
                destinationPath,
                mode,
                cancellationToken,
                logSegmentStart,
                logSegmentStop,
                logSegmentFailed,
                (offset, endoffset, ct) => segmentFactory(offset, ct),
                bufferFactory);
        }

        public static async Task<long> Download(
            Configuration config,
            Uri uri,
            Stream httpStream,
            long? knownLength,
            string destinationPath,
            FileMode mode,
            CancellationToken cancellationToken,
            LogSegmentStart logSegmentStart,
            LogSegmentStop logSegmentStop,
            LogSegmentFailed logSegmentFailed,
            StreamSegmentFactory2 segmentFactory,
            Func<IPoolHandle<byte[]>> bufferFactory)
        {
            ArgumentUtility.CheckForNull(config, nameof(config));
            ArgumentUtility.CheckStringForNullOrEmpty(destinationPath, nameof(destinationPath));
            ArgumentUtility.CheckForNull(logSegmentStart, nameof(logSegmentStart));
            ArgumentUtility.CheckForNull(logSegmentStop, nameof(logSegmentStop));
            ArgumentUtility.CheckForNull(logSegmentFailed, nameof(logSegmentFailed));
            ArgumentUtility.CheckForNull(segmentFactory, nameof(segmentFactory));

            bool success = false;

            try
            {
                long? length;
                if (knownLength.HasValue)
                {
                    length = knownLength.Value;
                }
                else if (httpStream.CanSeek)
                {
                    length = httpStream.Length;
                }
                else
                {
                    length = (long?)null;
                }

                using (var wholeFile = FileStreamUtils.OpenFileStreamForAsync(destinationPath, mode, FileAccess.Write, FileShare.Write))
                {
                    if (length.HasValue && length.Value > config.SegmentSizeInBytes)
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

                        // Do NOT remove this line. FileStream has a bug where it is not
                        // thread-safe when resizing a file.
                        wholeFile.SetLength(length.Value);

                        Stream streamToConsume = httpStream;
                        await DownloadInSegmentsAsync(config, uri, wholeFile, destinationPath, length.Value, cancellationToken, logSegmentStart, logSegmentStop, logSegmentFailed, (start, end, ct) =>
                        {
                            // This lambda may get called multiple times for retries, so make sure
                            // that we don't keep returning the same broken stream.
                            if (start == 0 && streamToConsume != null)
                            {
                                Stream streamToReturn = streamToConsume;
                                streamToConsume = null;
                                return Task.FromResult(streamToReturn);
                            }

                            return segmentFactory(start, end, ct);
                        }, bufferFactory).ConfigureAwait(false);
                        success = true;

                        if (sparseFile)
                        {
                            int error;
                            if (0 != (error = AsyncFile.TryMarkSparse(wholeFile.SafeFileHandle, sparse: false)))
                            {
                                throw new IOException("Could not unmark file as sparse: " + error);
                            }
                        }

                        return length.Value;
                    }
                    else
                    {
                        long result = await DownloadSegmentAsync(config, uri, httpStream, wholeFile, destinationPath, 0, long.MaxValue, length,
                            logSegmentStart, logSegmentStop, cancellationToken, bufferFactory).ConfigureAwait(false);
                        success = true;
                        return result;
                    }
                }
            }
            finally
            {
                if (!success && File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
            }
        }

        private static async Task DownloadInSegmentsAsync(
            Configuration config,
            Uri uri,
            FileStream wholeFile,
            string destinationPath,
            long fileLength,
            CancellationToken cancellationToken,
            LogSegmentStart logSegmentStart,
            LogSegmentStop logSegmentStop,
            LogSegmentFailed logSegmentFailed,
            StreamSegmentFactory2 segmentStreamFactory,
            Func<IPoolHandle<byte[]>> bufferFactory)
        {
            int segments = (int)((fileLength + config.SegmentSizeInBytes - 1) / config.SegmentSizeInBytes);

            var downloadBlock = NonSwallowingActionBlock.Create<int>(
                async (segmentIndex) =>
                {
                    long startPosition = config.SegmentSizeInBytes;
                    startPosition *= segmentIndex;
                    long endPosition = Math.Min(startPosition + config.SegmentSizeInBytes, fileLength);

                    var traceToCallback = new CallbackAppTraceSource(
                        traceMessageCallback: (string message) => logSegmentFailed.Invoke(destinationPath, startPosition, endPosition, message),
                        leastSevereLevelToTrace: SourceLevels.Information);

                    await AsyncHttpRetryHelper.InvokeVoidAsync(
                        async () =>
                        {
                            using (Stream httpSegmentStream = await segmentStreamFactory(startPosition, endPosition, cancellationToken).ConfigureAwait(false))
                            {
                                await DownloadSegmentAsync(config, uri, httpSegmentStream, wholeFile, destinationPath, startPosition, endPosition,
                                    endPosition - startPosition, logSegmentStart, logSegmentStop, cancellationToken, bufferFactory).ConfigureAwait(false);
                            }
                        },
                        config.MaxSegmentDownloadRetries,
                        traceToCallback,
                        cancellationToken,
                        continueOnCapturedContext: false,
                        context: $"{uri} [{startPosition}-{endPosition}]");
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = config.MaxParallelSegmentDownloadsPerFile,
                    CancellationToken = cancellationToken,
                });

            await downloadBlock.SendAllAndCompleteSingleBlockNetworkAsync(Enumerable.Range(0, segments), cancellationToken).ConfigureAwait(false);
        }

        private static async Task<long> DownloadSegmentAsync(
            Configuration config,
            Uri uri,
            Stream httpSegmentStream,
            FileStream wholeFile,
            string destinationPath,
            long startPosition,
            long maxEndPosition,
            long? expectedDownloadLength,
            LogSegmentStart logSegmentStart,
            LogSegmentStop logSegmentStop,
            CancellationToken cancellationToken,
            Func<IPoolHandle<byte[]>> bufferFactory)
        {
            logSegmentStart(destinationPath, startPosition, expectedDownloadLength ?? maxEndPosition);

            long totalBytesRead = 0;
            long currentPosition = startPosition;

            using (var segmentTimeoutCancellation = new CancellationTokenSource(config.SegmentDownloadTimeout))
            using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, segmentTimeoutCancellation.Token))
            using (var buffer = bufferFactory())
            {
                while (currentPosition < maxEndPosition)
                {
                    int bytesToRead = (int)Math.Min(buffer.Value.Length, maxEndPosition - currentPosition);

                    int bytesRead = await httpSegmentStream
                        .ReadToBufferAsync(new ArraySegment<byte>(buffer.Value, 0, bytesToRead), linkedCancellationTokenSource.Token)
                        .EnforceCancellation(linkedCancellationTokenSource.Token, () => $"Timed out reading from '{uri.AbsoluteUri}'.").ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await AsyncFile
                        .WriteAsync(wholeFile, currentPosition, new ArraySegment<byte>(buffer.Value, 0, bytesRead))
                        .EnforceCancellation(linkedCancellationTokenSource.Token, () => $"Timed out writing to '{wholeFile.Name}'.").ConfigureAwait(false);

                    currentPosition += bytesRead;
                    totalBytesRead += bytesRead;
                }
            }

            logSegmentStop(destinationPath, startPosition, startPosition + totalBytesRead);

            if (expectedDownloadLength.HasValue)
            {
                if (expectedDownloadLength.Value != totalBytesRead)
                {
                    throw new EndOfStreamException($"Reached end of stream at {totalBytesRead} bytes of the {expectedDownloadLength.Value} expected.");
                }
            }

            return totalBytesRead;
        }
    }
}
