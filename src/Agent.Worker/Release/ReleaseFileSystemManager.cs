using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ReleaseFileSystemManager))]
    public interface IReleaseFileSystemManager : IAgentService
    {
        StreamReader GetFileReader(string filePath);

        Task WriteStreamToFile(Stream stream, string filePath, int bufferSize, CancellationToken cancellationToken);

        void EnsureEmptyDirectory(string directoryPath, CancellationToken cancellationToken);

        void EnsureDirectoryExists(string directoryPath);

        void EnsureParentDirectory(string filePath);

        void DeleteFile(string filePath);

        void MoveFile(string sourceFileName, string destFileName);

        void CreateEmptyFile(string filePath);

        string GetFileName(string filePath);

        string JoinPath(string rootDirectory, string relativePath);
    }

    public class ReleaseFileSystemManager : AgentService, IReleaseFileSystemManager
    {
        public void EnsureEmptyDirectory(string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                var path = ValidatePath(directoryPath);
                if (Directory.Exists(path))
                {
                    IOUtil.DeleteDirectory(path, cancellationToken);
                }

                EnsureDirectoryExists(path);
            }
            catch (Exception ex)
            {
                var exception = ex;
                
                if (ex is AggregateException)
                {
                    exception = ((AggregateException)ex).Flatten().InnerException;
                }

                if (exception is DirectoryNotFoundException ||
                    exception is UnauthorizedAccessException ||
                    exception is IOException ||
                    exception is OperationCanceledException)
                {
                    throw new ArtifactDirectoryCreationFailedException(StringUtil.Loc("RMFailedCreatingArtifactDirectory", directoryPath), exception);
                }
                else
                {
                    throw;
                }
            }
        }

        public StreamReader GetFileReader(string filePath)
        {
            string path = Path.Combine(ValidatePath(filePath));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(StringUtil.Loc("FileNotFound", path));
            }

            return new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultStreamBufferSize, true));
        }

        private static string ValidatePath(string path)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            return Path.GetFullPath(path);
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            string path = ValidatePath(directoryPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void EnsureParentDirectory(string filePath)
        {
            DirectoryInfo ensureParentDirectory = Directory.GetParent(filePath);
            EnsureDirectoryExists(ensureParentDirectory.FullName);
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void MoveFile(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public void CreateEmptyFile(string filePath)
        {
            using (new FileStream(filePath, FileMode.Create))
            {
            }
        }

        public string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public string JoinPath(string rootDirectory, string relativePath)
        {
            return Path.Combine(rootDirectory, relativePath);
        }

        public async Task WriteStreamToFile(Stream stream, string filePath, int bufferSize, CancellationToken cancellationToken)
        {
            ArgUtil.NotNull(stream, nameof(stream));
            ArgUtil.NotNullOrEmpty(filePath, nameof(filePath));

            EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            using (var targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                await stream.CopyToAsync(targetStream, bufferSize, cancellationToken);
            }
        }

        private const int DefaultStreamBufferSize = 8192;
    }
}