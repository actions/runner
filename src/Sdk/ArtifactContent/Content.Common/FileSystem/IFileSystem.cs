using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Abstraction for the file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Creates a directory.
        /// </summary>
        void CreateDirectory(string directoryPath);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        void DeleteFile(string filePath);

        /// <summary>
        /// Returns whether a directory exists.
        /// </summary>
        /// <param name="directoryPath">Path to directory.</param>
        bool DirectoryExists(string directoryPath);

        /// <summary>
        /// Returns a list of file names under the path specified, and optionally within all subdirectories.
        /// </summary>
        /// <param name="directoryFullPath">The directory to search</param>
        /// <param name="recursiveSearch">Specifies whether the search operation should include only the current directory or all subdirectories</param>
        /// <returns>Full file paths.</returns>
        IEnumerable<string> EnumerateFiles(string directoryFullPath, bool recursiveSearch);

        /// <summary>
        /// Returns a list of directory names under the path specified, and optionally all subdirectories
        /// </summary>
        /// <param name="directoryFullPath">The directory to search</param>
        /// <param name="recursiveSearch">Specifies whether the search operation should include only the currect directory or all subdirectories</param>
        /// <returns>A list of all subdirectories</returns>
        IEnumerable<string> EnumerateDirectories(string directoryFullPath, bool recursiveSearch);

        /// <summary>
        /// Returns whether a file exists.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        bool FileExists(string filePath);

        /// <summary>
        /// Geneerates a random name for a file.
        /// </summary>
        string GetRandomFileName();

        /// <summary>
        /// Gets the full path to a temporary file.
        /// </summary>
        string GetTempFileFullPath();

        /// <summary>
        /// Gets the full path to the temporary folder in the file system.
        /// </summary>
        string GetTempFullPath();

        /// <summary>
        /// Gets the full path to the working directory of the executing process. 
        /// </summary>
        string GetWorkingDirectory();

        /// <summary>
        /// Opens up a text reader to a specific file in the file system.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>A text reader on the filde.</returns>
        StreamReader OpenText(string filePath);

        /// <summary>
        /// Opens stream given a file path.
        /// </summary>
        Stream OpenStreamForFile(string fileFullPath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        /// <summary>
        /// Opens file stream given a file path.
        /// </summary>
        /// <remarks>Some places require specifically a FileStream rather than a Stream.</remarks>
        FileStream OpenFileStreamForAsync(string fileFullPath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        /// <summary>
        /// Reads all text from a file.
        /// </summary>
        string ReadAllText(string filePath);

        /// <summary>
        /// Reads all bytes from a file.
        /// </summary>
        byte[] ReadAllBytes(string filePath);
        
        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        void WriteAllBytes(string filePath, byte[] bytes);

        /// <summary>
        /// Writes all text to a file.
        /// </summary>
        void WriteAllText(string filePath, string content);
    }
}
