using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ZipStreamDownloader))]
    public interface IZipStreamDownloader : IAgentService
    {
        Task<int> DownloadFromStream(
            Stream zipStream,
            string folderWithinStream,
            string relativePathWithinStream,
            string localFolderPath);
    }

    public class ZipStreamDownloader : AgentService, IZipStreamDownloader
    {
        private const char ForwardSlash = '/';

        private const char Backslash = '\\';

        public Task<int> DownloadFromStream(Stream zipStream, string folderWithinStream, string relativePathWithinStream, string localFolderPath)
        {
            this.Trace.Entering();

            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));
            ArgUtil.NotNullOrEmpty(folderWithinStream, nameof(folderWithinStream));

            return this.DownloadStreams(zipStream, localFolderPath, folderWithinStream, relativePathWithinStream, null);
        }

        public async Task DownloadFromStream(Stream zipStream, string folderWithinStream, string relativePathWithinStream, Action<string, Stream> streamDownloaderAction)
        {
            this.Trace.Entering();

            ArgUtil.NotNull(streamDownloaderAction, nameof(streamDownloaderAction));
            ArgUtil.NotNullOrEmpty(folderWithinStream, nameof(folderWithinStream));

            await this.DownloadStreams(zipStream, null, folderWithinStream, relativePathWithinStream, streamDownloaderAction);
        }

        private async Task<int> DownloadStreams(Stream zipStream, string localFolderPath, string folderWithinStream, string relativePathWithinStream, Action<string, Stream> streamDownloaderAction)
        {
            this.Trace.Entering();

            int streamsDownloaded = 0;
            var fileSystemManager = this.HostContext.CreateService<IReleaseFileSystemManager>();

            foreach (var stream in GetZipEntryStreams(zipStream))
            {
                try
                {
                    // Remove leading '/'s if any 
                    var path = stream.FullName.TrimStart(ForwardSlash);

                    this.Trace.Verbose($"Downloading {path}");
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

                    if (streamDownloaderAction != null)
                    {
                        streamDownloaderAction(path, stream.ZipStream);
                    }
                    else
                    {
                        await fileSystemManager.WriteStreamToFile(stream.ZipStream, Path.Combine(localFolderPath, path));
                    }

                    streamsDownloaded++;
                }
                finally
                {
                    stream.ZipStream.Dispose();
                }
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
    }

    internal class ZipEntryStream
    {
        public string FullName { get; set; }

        public Stream ZipStream { get; set; }
    }
}