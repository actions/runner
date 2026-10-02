using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GitHub.Runner.Sdk
{
    public static class PathUtil
    {
#if OS_WINDOWS
        public static readonly string PathVariable = "Path";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            System.IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            System.IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetFinalPathNameByHandle(
            SafeFileHandle hFile,
            [Out] StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        private const uint FILE_READ_ATTRIBUTES = 0x80;
        private const uint FILE_SHARE_READ = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint FILE_SHARE_DELETE = 0x4;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const uint VOLUME_NAME_DOS = 0x0;

        /// <summary>
        /// Returns the NTFS canonical path for a directory, resolving drive letter
        /// and folder name casing to match what is stored on disk.
        /// On non-Windows platforms, returns the path unchanged.
        /// </summary>
        public static string GetCanonicalPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return path;
            }

            using var handle = CreateFile(
                path,
                FILE_READ_ATTRIBUTES,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                System.IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS,
                System.IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return path;
            }

            var buffer = new StringBuilder(1024);
            var result = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, VOLUME_NAME_DOS);
            if (result == 0)
            {
                return path;
            }

            // Retry with a larger buffer if the path was longer than expected
            if (result >= buffer.Capacity)
            {
                buffer = new StringBuilder((int)result + 1);
                result = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, VOLUME_NAME_DOS);
                if (result == 0 || result >= buffer.Capacity)
                {
                    return path;
                }
            }

            var canonicalPath = buffer.ToString();

            // Strip the \\?\UNC\ prefix and convert to standard UNC path
            if (canonicalPath.StartsWith(@"\\?\UNC\", System.StringComparison.Ordinal))
            {
                canonicalPath = @"\\" + canonicalPath.Substring(8);
            }
            // Strip the \\?\ prefix for local paths
            else if (canonicalPath.StartsWith(@"\\?\", System.StringComparison.Ordinal))
            {
                canonicalPath = canonicalPath.Substring(4);
            }

            return canonicalPath;
        }
#else
        public static readonly string PathVariable = "PATH";

        public static string GetCanonicalPath(string path)
        {
            return path;
        }
#endif

        public static string PrependPath(string path, string currentPath)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            if (string.IsNullOrEmpty(currentPath))
            {
                // Careful not to add a trailing separator if the PATH is empty.
                // On OSX/Linux, a trailing separator indicates that "current directory"
                // is added to the PATH, which is considered a security risk.
                return path;
            }

            // Not prepend path if it is already the first path in %PATH%
            if (currentPath.StartsWith(path + Path.PathSeparator, IOUtil.FilePathStringComparison))
            {
                return currentPath;
            }
            else
            {
                return path + Path.PathSeparator + currentPath;
            }
        }
    }
}
