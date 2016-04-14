using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ReleaseFileSystemManager))]
    public interface IReleaseFileSystemManager : IAgentService
    {
        StreamReader GetFileReader(string filePath);

        Task WriteStreamToFile(Stream stream, string filePath);

        void CleanupDirectory(string directoryPath, CancellationToken cancellationToken);
    }

    public class ReleaseFileSystemManager : AgentService, IReleaseFileSystemManager
    {
        private const int StreamBufferSize = 1024;

        public void CleanupDirectory(string directoryPath, CancellationToken cancellationToken)
        {
            var path = ValidatePath(directoryPath);
            if (Directory.Exists(path))
            {
                IOUtil.DeleteDirectory(path, cancellationToken);
            }

            EnsureDirectoryExists(path);
        }

        public StreamReader GetFileReader(string filePath)
        {
            string path = Path.Combine(ValidatePath(filePath));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("fileName");
            }

            return new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, true));
        }

        private static string ValidatePath(string path)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            return Path.GetFullPath(path);
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            string path = ValidatePath(directoryPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public async Task WriteStreamToFile(Stream stream, string filePath)
        {
            ArgUtil.NotNull(stream, nameof(stream));
            ArgUtil.NotNullOrEmpty(filePath, nameof(filePath));

            EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            using (var targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, true))
            {
                await stream.CopyToAsync(targetStream, StreamBufferSize);
            }
        }
    }
}