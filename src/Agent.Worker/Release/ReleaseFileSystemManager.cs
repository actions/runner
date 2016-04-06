using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ReleaseFileSystemManager))]
    public interface IReleaseFileSystemManager : IAgentService
    {
        IEnumerable<FileInfo> GetFiles(string directoryPath, SearchOption searchOption);

        StreamReader GetFileReader(string filePath);

        Task WriteStreamToFile(Stream stream, string filePath);

        void DeleteDirectory(string directoryPath);
    }

    public class ReleaseFileSystemManager : AgentService, IReleaseFileSystemManager
    {
        private readonly IDictionary<string, DirectoryInfo> directories
            = new Dictionary<string, DirectoryInfo>(StringComparer.OrdinalIgnoreCase);

        private RetryExecutor _retryExecutor = new RetryExecutor();
        private const int StreamBufferSize = 1024;

        public IEnumerable<FileInfo> GetFiles(string directoryPath, SearchOption searchOption)
        {
            return
                Directory.GetFiles(ValidatePath(directoryPath), "*", searchOption)
                    .Select(fullPath => new FileInfo(fullPath));
        }

        public IEnumerable<string> GetDirectories(string directoryPath, SearchOption searchOption)
        {
            return Directory.GetDirectories(ValidatePath(directoryPath), "*", searchOption);
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public void DeleteFile(string filePath)
        {
            _retryExecutor.Execute(File.Delete, filePath);
        }

        public void DeleteDirectory(string directoryPath)
        {
            _retryExecutor.Execute(path => { Directory.Delete(path, true); }, directoryPath);
        }

        public StreamReader GetFileReader(string filePath)
        {
            string path = Path.Combine(ValidatePath(filePath));
            if (!this.FileExists(path))
            {
                throw new ArgumentOutOfRangeException("fileName");
            }

            return new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read));
        }

        public StreamWriter GetFileWriter(string filePath)
        {
            return new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write));
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

            this.EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[StreamBufferSize];
                int count;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, count);
                }
            }
        }
    }
}