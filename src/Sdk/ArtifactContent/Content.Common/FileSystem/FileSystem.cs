using GitHub.Services.Common;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Physical file system.
    /// </summary>
    public class FileSystem : FileSystemBase, IFileSystem
    {
        public static readonly FileSystem Instance = new FileSystem();

        private FileSystem() { }

        public void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        public bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public IEnumerable<string> EnumerateDirectories(string directoryFullPath, bool recursiveSearch)
        {
            return LongPathUtility.EnumerateDirectories(directoryFullPath, recursiveSearch);
        }

        public IEnumerable<string> EnumerateFiles(string directoryFullPath, bool recursiveSearch)
        {
            return LongPathUtility.EnumerateFiles(directoryFullPath, recursiveSearch);
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public string GetTempFileFullPath()
        {
            return Path.GetTempFileName();
        }

        public string GetTempFullPath()
        {
            return Path.GetTempPath();
        }

        public string GetWorkingDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        // This is a method to return a general purpose stream.
        public Stream OpenStreamForFile(string fileFullPath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return FileStreamUtils.OpenFileStreamForAsync(fileFullPath, fileMode, fileAccess, fileShare);
        }

        public FileStream OpenFileStreamForAsync(string fileFullPath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return FileStreamUtils.OpenFileStreamForAsync(fileFullPath, fileMode, fileAccess, fileShare);
        }

        public StreamReader OpenText(string filePath)
        {
            return File.OpenText(filePath);
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public byte[] ReadAllBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            File.WriteAllBytes(filePath, bytes);
        }

        public void WriteAllText(string filePath, string content)
        {
            File.WriteAllText(filePath, content);
        }
    }
}
