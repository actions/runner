using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides path normalization/expansion for absolute, relative and UNC-style paths
    /// and supports paths that contain more than 248 characters.
    /// </summary>
    /// <remarks>
    /// This utility class can be used in place of the .NET Path and Directory classes
    /// that throw System.IO.PathTooLongException when paths are longer than 248 characters
    /// </remarks>
    public static class LongPathUtility
    {
        private static Regex AbsolutePathRegEx = new Regex(@"^([a-zA-Z]:\\|\\\\)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private const int ERROR_FILE_NOT_FOUND = 2;

        /// <summary>
        /// Returns a list of directory names under the path specified, and optionally all subdirectories
        /// </summary>
        /// <param name="path">The directory to search</param>
        /// <param name="recursiveSearch">Specifies whether the search operation should include only the currect directory or all subdirectories</param>
        /// <returns>A list of all subdirectories</returns>
        public static IEnumerable<string> EnumerateDirectories(string path, bool recursiveSearch)
        {
            var directoryPaths = new List<string>();
            EnumerateDirectoriesInternal(directoryPaths, path, recursiveSearch);
            return directoryPaths;
        }

        /// <summary>
        /// Returns a list of file names under the path specified, and optionally within all subdirectories.
        /// </summary>
        /// <param name="path">The directory to search</param>
        /// <param name="recursiveSearch">Specifies whether the search operation should include only the current directory or all subdirectories</param>
        /// <returns>
        /// A list of full file names(including path) contained in the directory specified that match the specified search pattern.</returns>
        public static IEnumerable<string> EnumerateFiles(string path, bool recursiveSearch)
        {
            return EnumerateFiles(path, "*", recursiveSearch);
        }

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified path,
        /// and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search</param>
        /// <param name="matchPattern">The search string to match against the names of the files</param>
        /// <param name="recursiveSearch">Specifies whether the search operation should include only the current directory or all subdirectories</param>
        /// <returns>
        /// A list of full file names(including path) contained in the directory specified (and subdirectories optionally) that match the specified pattern.
        /// </returns>
        public static IEnumerable<string> EnumerateFiles(string path, string matchPattern, bool recursiveSearch)
        {
            if (!DirectoryExists(path))
            {
                throw new DirectoryNotFoundException($"The path '{path}' is not a valid directory.");
            }

            var filePaths = new List<string>();
            EnumerateFilesInternal(filePaths, path, matchPattern, recursiveSearch);
            return filePaths;
        }

        /// <summary>
        /// Returns true/false whether the file exists.  This method inspects the
        /// file system attributes and supports files without extensions (ex:  DIRS, Sources).  This method
        /// supports file paths that are longer than 260 characters.
        /// </summary>
        /// <param name="filePath">The file path to inspect</param>
        /// <returns>
        /// True if the file exists or false if not
        /// </returns>
        public static bool FileExists(string filePath)
        {
            return FileOrDirectoryExists(filePath, isDirectory: false);
        }

        /// <summary>
        /// Returns true/false whether the directory exists.  This method inspects the
        /// file system attributes and supports files without extensions (ex:  DIRS, Sources).  This method
        /// supports file paths that are longer than 260 characters.
        /// </summary>
        /// <param name="directoryPath">The file path to inspect</param>
        /// <returns>
        /// True if the directory exists or false if not
        /// </returns>
        public static bool DirectoryExists(string directoryPath)
        {
            return FileOrDirectoryExists(directoryPath, isDirectory: true);
        }

        private static bool FileOrDirectoryExists(string filePath, bool isDirectory)
        {
            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A path to the file is required and cannot be null, empty or whitespace", "filePath");
            }

            bool pathExists = false;

            // File names may or may not include an extension (ex:  DIRS, Sources). We have to look at the attributes
            // on the file system object in order to distinguish a directory from a file
            var attributes = (FlagsAndAttributes)NativeMethods.GetFileAttributes(filePath);

            if (attributes != FlagsAndAttributes.InvalidFileAttributes)
            {
                bool pathIsDirectory = (attributes & FlagsAndAttributes.Directory) == FlagsAndAttributes.Directory;

                if (pathIsDirectory == isDirectory)
                {
                    pathExists = true;
                }
            }

            return pathExists;
        }

        /// <summary>
        /// Returns the fully expanded/normalized path.  This method supports paths that are
        /// longer than 248 characters.
        /// </summary>
        /// <param name="path">The file or directory path</param>
        /// <returns></returns>
        public static string GetFullNormalizedPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A path is required and cannot be null, empty or whitespace", "path");
            }

            string outPath = path;

            // We need the length of the absolute path in order to prepare a buffer of
            // the correct size
            uint bufferSize = NativeMethods.GetFullPathName(path, 0, null, null);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (bufferSize > 0)
            {
                var absolutePath = new StringBuilder((int)bufferSize);
                uint length = NativeMethods.GetFullPathName(path, bufferSize, absolutePath, null);
                lastWin32Error = Marshal.GetLastWin32Error();

                if (length > 0)
                {
                    outPath = absolutePath.ToString();
                }
                else
                {
                    // Path resolution failed
                    throw new Win32Exception(
                        lastWin32Error,
                        String.Format(
                            CultureInfo.InvariantCulture,
                            "Path normalization/expansion failed.  The path length was not returned by the Kernel32 subsystem for '{0}'.",
                            path
                        )
                    );
                }
            }
            else
            {
                // Path resolution failed and the path length could not 
                // be determined
                throw new Win32Exception(
                    lastWin32Error,
                    String.Format(
                        CultureInfo.InvariantCulture,
                        "Path normalization/expansion failed. A full path was not returned by the Kernel32 subsystem for '{0}'.",
                        path
                    )
                );
            }

            return outPath != null ? outPath.TrimEnd('\\') : null;
        }

        /// <summary>
        /// Determines whether the specified path is an absolute path or not.
        /// </summary>
        /// <param name="path">The path to be tested.</param>
        /// <returns>
        ///   <c>true</c> if the path is absolute; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAbsolutePath(string path)
        {
            return LongPathUtility.AbsolutePathRegEx.Match(path).Success;
        }

        public static string RemoveExtendedLengthPathPrefix(string inPath)
        {
            string outPath = inPath;

            if (!String.IsNullOrWhiteSpace(inPath))
            {
                if (inPath.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                {
                    // ex: \\?\UNC\server\share to \\server\share
                    outPath = inPath.Replace(@"\\?\UNC", @"\");

                    // ex: \\?\c:\windows to c:\windows
                    outPath = outPath.Replace(@"\\?\", String.Empty);
                }
            }

            return outPath;
        }

        private static string CombinePaths(string pathA, string pathB)
        {
            if (pathA == null)
            {
                throw new ArgumentNullException("pathA");
            }

            if (pathB == null)
            {
                throw new ArgumentNullException("pathB");
            }

            // The Path class does not suffer from the 248/260 character limitation
            // that the File and Directory classes do.
            return Path.Combine(
                pathA.TrimEnd('\\'),
                pathB.TrimStart('\\')
            );
        }

        private static string ConvertToExtendedLengthPath(string path)
        {
            string extendedLengthPath = GetFullNormalizedPath(path);

            if (!String.IsNullOrWhiteSpace(extendedLengthPath))
            {
                //no need to modify- it's already unicode
                if (!extendedLengthPath.StartsWith(@"\\?", StringComparison.OrdinalIgnoreCase))
                {
                    // ex: \\server\share
                    if (extendedLengthPath.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
                    {
                        // make it \\?\UNC\server\share
                        extendedLengthPath = String.Format(
                            CultureInfo.InvariantCulture,
                            @"\\?\UNC{0}",
                            extendedLengthPath.Substring(1)
                        );
                    }
                    else //not unicode already, and not UNC
                    {
                        extendedLengthPath = String.Format(
                            CultureInfo.InvariantCulture,
                            @"\\?\{0}",
                            extendedLengthPath
                        );
                    }
                }
            }

            return extendedLengthPath;
        }

        private static IEnumerable<string> EnumerateDirectoriesInPath(string path)
        {
            SafeFindHandle handle = null;
            var findData = new FindData();
            var childDirectories = new List<string>();

            using (handle = NativeMethods.FindFirstFile(CombinePaths(ConvertToExtendedLengthPath(path), "*"), findData))
            {
                if (!handle.IsInvalid)
                {
                    bool searchComplete = false;

                    do
                    {
                        // skip the dot directories
                        if (!findData.fileName.Equals(@".") && !findData.fileName.Equals(@".."))
                        {
                            if ((findData.fileAttributes & (int)FileAttributes.Directory) != 0)
                            {
                                childDirectories.Add(RemoveExtendedLengthPathPrefix(CombinePaths(path, findData.fileName)));
                            }
                        }

                        if (NativeMethods.FindNextFile(handle, findData))
                        {
                            if (handle.IsInvalid)
                            {
                                throw new Win32Exception(
                                    Marshal.GetLastWin32Error(),
                                    String.Format(
                                        CultureInfo.InvariantCulture,
                                        "Enumerating subdirectories for path '{0}' failed.",
                                        path
                                    )
                                );
                            }
                        }
                        else
                        {
                            searchComplete = true;
                        }

                    } while (!searchComplete);
                }
            }

            return childDirectories;
        }

        private static IEnumerable<string> EnumerateFilesInPath(string path, string matchPattern)
        {
            SafeFindHandle handle = null;
            var findData = new FindData();
            var fullFilePaths = new List<string>();

            using (handle = NativeMethods.FindFirstFile(CombinePaths(ConvertToExtendedLengthPath(path), matchPattern), findData))
            {
                int lastWin32Error = Marshal.GetLastWin32Error();

                if (handle.IsInvalid)
                {
                    if (lastWin32Error != ERROR_FILE_NOT_FOUND)
                    {
                        throw new Win32Exception(
                            lastWin32Error,
                            String.Format(CultureInfo.InvariantCulture, "Enumerating files for path '{0}' failed.", path)
                        );
                    }
                }
                else
                {
                    bool searchComplete = false;

                    do
                    {
                        // skip the dot directories
                        if (!findData.fileName.Equals(@".") && !findData.fileName.Equals(@".."))
                        {
                            if ((findData.fileAttributes & (int)FileAttributes.Directory) == 0)
                            {
                                fullFilePaths.Add(RemoveExtendedLengthPathPrefix(CombinePaths(path, findData.fileName)));
                            }
                        }

                        if (NativeMethods.FindNextFile(handle, findData))
                        {
                            lastWin32Error = Marshal.GetLastWin32Error();

                            if (handle.IsInvalid)
                            {
                                throw new Win32Exception(
                                    lastWin32Error,
                                    String.Format(
                                        CultureInfo.InvariantCulture,
                                        "Enumerating subdirectories for path '{0}' failed.",
                                        path
                                    )
                                );
                            }
                        }
                        else
                        {
                            searchComplete = true;
                        }

                    } while (!searchComplete);
                }
            }

            return fullFilePaths;
        }

        private static void EnumerateFilesInternal(List<string> filePaths, string path, string matchPattern, bool recursiveSearch)
        {
            var fullFilePaths = EnumerateFilesInPath(path, matchPattern);
            if (fullFilePaths.Any())
            {
                lock (filePaths)
                {
                    filePaths.AddRange(fullFilePaths);
                }
            }

            if (recursiveSearch)
            {
                var directorySearchPaths = EnumerateDirectoriesInPath(path);
                if (directorySearchPaths.Any())
                {
                    Parallel.ForEach(
                        directorySearchPaths,
                        (searchPath) =>
                        {
                            EnumerateFilesInternal(filePaths, searchPath, matchPattern, recursiveSearch);
                        }
                    );
                }
            }
        }

        public static void EnumerateDirectoriesInternal(List<string> directoryPaths, string path, bool recursiveSearch)
        {
            var directorySearchPaths = EnumerateDirectoriesInPath(path);
            if (directorySearchPaths.Any())
            {
                lock (directoryPaths)
                {
                    directoryPaths.AddRange(directorySearchPaths);
                }

                if (recursiveSearch)
                {
                    // This will not ensure that the directory paths are added to the list
                    // in alphabetical order but does provide performance 2 - 4 times better than the
                    // canonical Directory.GetDirectories() method.
                    Parallel.ForEach(
                        directorySearchPaths,
                        (searchPath) =>
                        {
                            EnumerateDirectoriesInternal(directoryPaths, searchPath, recursiveSearch);
                        }
                    );
                }
            }
        }

        /// <summary>
        /// Kernel32.dll native interop methods for use with utility file/path parsing
        /// operations
        /// </summary>
        private static class NativeMethods
        {
            private const string Kernel32Dll = "kernel32.dll";

            [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FindClose(IntPtr hFindFile);

            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "FindData.alternateFileName")]
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "FindData.fileName")]
            [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            public static extern SafeFindHandle FindFirstFile(
                [MarshalAs(UnmanagedType.LPTStr)]
                string fileName,
                [In, Out] FindData findFileData
            );
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "FindData.alternateFileName")]
            [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "FindData.fileName")]
            [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FindNextFile(SafeFindHandle hFindFile, [In, Out] FindData lpFindFileData);

            [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            public static extern int GetFileAttributes(string lpFileName);

            [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            public static extern uint GetFullPathName(
                [MarshalAs(UnmanagedType.LPTStr)]
                string lpFileName,
                uint nBufferLength,
                [Out]
                StringBuilder lpBuffer,
                StringBuilder lpFilePart
            );
        }

        //for mapping to the WIN32_FIND_DATA native structure
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed.")]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class FindData
        {
            // NOTE:
            // Although it may seem correct to Marshal the string members of this class as UnmanagedType.LPWStr, they
            // must explicitly remain UnmanagedType.ByValTStr with the size constraints noted.  Otherwise we end up with
            // COM Interop exceptions while trying to marshal the data across the PInvoke boundaries.  We thus require the StyleCop
            // suppressions on the NativeMethods.FindNextFile() method above.
            public int fileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME creationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME lastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string fileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string alternateFileName;
        }

        //A Win32 safe find handle in which a return value of -1 indicates it's invalid
        private sealed class SafeFindHandle : Microsoft.Win32.SafeHandles.SafeHandleMinusOneIsInvalid
        {
            public SafeFindHandle()
                : base(true)
            {
                return;
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            protected override bool ReleaseHandle()
            {
                return NativeMethods.FindClose(handle);
            }
        }

        [Flags]
        private enum FlagsAndAttributes : uint
        {
            None = 0x00000000,
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000,

            InvalidFileAttributes = 0xFFFFFFFF // Returned by GetFileAttributes on Non existant path
        }
    }
}
