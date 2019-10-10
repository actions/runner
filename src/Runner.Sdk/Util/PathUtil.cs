using System;
using System.IO;

namespace GitHub.Runner.Sdk
{
    public static class PathUtil
    {
#if OS_WINDOWS
        public static readonly string PathVariable = "Path";
#else
        public static readonly string PathVariable = "PATH";
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
