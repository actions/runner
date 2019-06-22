using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitHub.Services.Common;

namespace GitHub.Services.ArtifactServices.App.Shared
{
    /// <summary>
    /// Implements a reader which processes an input text file which contains a list of files to be enumerated.
    /// Enforces that each line in the input text file:
    /// - is blank (in which case it is ignored)
    /// OR
    /// - begins with a hash sign (#) in which case it ignores
    /// OR is treated as a filename which
    /// - must be an absolute path
    /// - refers to a file which must exist
    /// - refers to a file which must falls under a common root directory provided
    /// </summary>
    public static class ListOfFiles
    {
        /// <summary>
        /// Static file list loader for when a list is desired.
        /// </summary>
        /// <param name="inputFilename">The input filename.</param>
        /// <param name="expectedCommonRootDirectory">The expected common root directory.</param>
        /// <returns></returns>
        public static Task<List<FileInfo>> LoadFileListAsync(string inputFilename, string requiredCommonRootDirectory = null)
        {
            FileInfo inputFile = new FileInfo(inputFilename);
            if (!inputFile.Exists)
            {
                throw new FileNotFoundException($"File to be read ({inputFilename}) does not exist", inputFilename);
            }


            DirectoryInfo rootDirectory = null;
            if (requiredCommonRootDirectory != null)
            {
                rootDirectory = new DirectoryInfo(LongPathUtility.GetFullNormalizedPath(requiredCommonRootDirectory));
                if (!rootDirectory.Exists)
                {
                    throw new DirectoryNotFoundException($"Root directory ({requiredCommonRootDirectory}) does not exist.");
                }
            }

            return ListOfFiles.EnumerateFilesAsync(inputFile, rootDirectory);
        }

        /// <summary>
        /// Gets a new enumeration of the files.
        /// </summary>
        /// <returns>An list of the files contained in the file.</returns>
        private async static Task<List<FileInfo>> EnumerateFilesAsync(FileInfo fileListFile, DirectoryInfo requiredCommonRootDirectory = null)
        {
            List<FileInfo> filesRead = new List<FileInfo>();

            using (StreamReader reader = new StreamReader(fileListFile.FullName))
            {
                string line;

                while ((line = (await reader.ReadLineAsync().ConfigureAwait(false))?.Trim()) != null)
                {
                    if (ListOfFiles.ShouldSkipLine(line))
                    {
                        continue;
                    }

                    FileInfo file = ListOfFiles.GetFileInfo(line);

                    if (requiredCommonRootDirectory != null && !file.FullName.StartsWith(requiredCommonRootDirectory.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"Only paths begining with directory '{requiredCommonRootDirectory.FullName}' are accepted in filelist, File: '{file.FullName}'");
                    }

                    filesRead.Add(file);
                }
            }

            return filesRead;
        }

        /// <summary>
        /// Throws if the provided line is not a valid entry for the file list, otherwise returns a FileInfo for the file.
        /// </summary>
        /// <param name="line">The line to process.</param>
        private static FileInfo GetFileInfo(string line)
        {
            if (!LongPathUtility.IsAbsolutePath(line))
            {
                throw new ArgumentException($"Only absolute paths are accepted in filelist. File: '{line}'");
            }

            // Construction of a FileInfo will throw for all invalid path characters, so we do not have to check that ourselves.
            FileInfo fileInfo = new FileInfo(line);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"File {line} does not exist.", line);
            }

            return fileInfo;
        }

        /// <summary>
        /// Determines if the provided line should be skipped during enumeration.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private static bool ShouldSkipLine(string line)
        {
            // The VSTS PublishSymbols task uses the # character to denote comments.
            return (line == string.Empty || line.StartsWith("#"));
        }
    }
}
