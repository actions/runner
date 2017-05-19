using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ZipStreamDownloader))]
    public interface IZipStreamDownloader : IAgentService
    {
        Task<int> DownloadFromStream(
            IExecutionContext executionContext,
            Stream zipStream,
            string folderWithinStream,
            string relativePathWithinStream,
            string localFolderPath);
    }

    // TODO: Add tests for this
    public class ZipStreamDownloader : AgentService, IZipStreamDownloader
    {
        private const char ForwardSlash = '/';

        private const char Backslash = '\\';

        public Task<int> DownloadFromStream(IExecutionContext executionContext, Stream zipStream, string folderWithinStream, string relativePathWithinStream, string localFolderPath)
        {
            Trace.Entering();

            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));
            ArgUtil.NotNull(folderWithinStream, nameof(folderWithinStream));

            return DownloadStreams(executionContext, zipStream, localFolderPath, folderWithinStream, relativePathWithinStream);
        }

        private async Task<int> DownloadStreams(IExecutionContext executionContext, Stream zipStream, string localFolderPath, string folderWithinStream, string relativePathWithinStream)
        {
            Trace.Entering();

            int streamsDownloaded = 0;
            var fileSystemManager = HostContext.CreateService<IReleaseFileSystemManager>();

            foreach (ZipEntryStream stream in GetZipEntryStreams(zipStream))
            {
                try
                {
                    // Remove leading '/'s if any 
                    var path = stream.FullName.TrimStart(ForwardSlash);

                    Trace.Verbose($"Downloading {path}");
                    if (!string.IsNullOrWhiteSpace(folderWithinStream))
                    {
                        var normalizedFolderWithInStream = folderWithinStream.TrimStart(ForwardSlash).TrimEnd(ForwardSlash) + ForwardSlash;

                        // If this zip entry does not start with the expected folderName, skip it. 
                        if (!path.StartsWith(normalizedFolderWithInStream, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        path = path.Substring(normalizedFolderWithInStream.Length);
                    }

                    if (!string.IsNullOrWhiteSpace(relativePathWithinStream)
                        && !relativePathWithinStream.Equals(ForwardSlash.ToString())
                        && !relativePathWithinStream.Equals(Backslash.ToString()))
                    {
                        var normalizedRelativePath =
                            relativePathWithinStream.Replace(Backslash, ForwardSlash).TrimStart(ForwardSlash).TrimEnd(ForwardSlash)
                            + ForwardSlash;

                        // Remove Blob Prefix path like "FabrikamFiber.DAL/bin/debug/" from the beginning of artifact full path
                        if (!path.StartsWith(normalizedRelativePath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        path = path.Substring(normalizedRelativePath.Length);
                    }

                    int bufferSize = executionContext.Variables.Release_Download_BufferSize ?? DefaultBufferSize;
                    await fileSystemManager.WriteStreamToFile(stream.ZipStream, Path.Combine(localFolderPath, path), bufferSize, executionContext.CancellationToken);

                    streamsDownloaded++;
                }
                finally
                {
                    stream.ZipStream.Dispose();
                }
            }

            if (streamsDownloaded == 0)
            {
                executionContext.Warning(StringUtil.Loc("RMArtifactEmpty"));
            }

            return streamsDownloaded;
        }

        private static IEnumerable<ZipEntryStream> GetZipEntryStreams(Stream zipStream)
        {
            return
                new ZipArchive(zipStream).Entries
                    .Where(entry => !entry.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                    .Select(entry => new ZipEntryStream { FullName = entry.FullName, ZipStream = entry.Open() });
        }

        private const int DefaultBufferSize = 8192;
    }

    internal class ZipEntryStream
    {
        public string FullName { get; set; }

        public Stream ZipStream { get; set; }
    }
}