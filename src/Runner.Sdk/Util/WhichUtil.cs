using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GitHub.Runner.Sdk
{
    public static class WhichUtil
    {
        public static string Which(string command, bool require = false, ITraceWriter trace = null, string prependPath = null)
        {
            ArgUtil.NotNullOrEmpty(command, nameof(command));
            trace?.Info($"Which2: '{command}'");
            if (Path.IsPathFullyQualified(command) && File.Exists(command))
            {
                trace?.Info($"Fully qualified path: '{command}'");
                return command;
            }
            string path = Environment.GetEnvironmentVariable(PathUtil.PathVariable);
            if (string.IsNullOrEmpty(path))
            {
                trace?.Info("PATH environment variable not defined.");
                path = path ?? string.Empty;
            }
            if (!string.IsNullOrEmpty(prependPath))
            {
                path = PathUtil.PrependPath(prependPath, path);
            }

            string[] pathSegments = path.Split(new Char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pathSegments.Length; i++)
            {
                pathSegments[i] = Environment.ExpandEnvironmentVariables(pathSegments[i]);
            }

            foreach (string pathSegment in pathSegments)
            {
                if (!string.IsNullOrEmpty(pathSegment) && Directory.Exists(pathSegment))
                {
                    string[] matches = null;
                    if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                        string pathExt = Environment.GetEnvironmentVariable("PATHEXT");
                        if (string.IsNullOrEmpty(pathExt))
                        {
                            // XP's system default value for PATHEXT system variable
                            pathExt = ".com;.exe;.bat;.cmd;.vbs;.vbe;.js;.jse;.wsf;.wsh";
                        }

                        string[] pathExtSegments = pathExt.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                        // if command already has an extension.
                        if (pathExtSegments.Any(ext => command.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        {
                            try
                            {
                                matches = Directory.GetFiles(pathSegment, command);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                trace?.Info("Ignore UnauthorizedAccess exception during Which.");
                                trace?.Verbose(ex.ToString());
                            }

                            if (matches != null && matches.Length > 0)
                            {
                                trace?.Info($"Location: '{matches.First()}'");
                                return matches.First();
                            }
                        }
                        else
                        {
                            string searchPattern;
                            searchPattern = StringUtil.Format($"{command}.*");
                            try
                            {
                                foreach (var file in Directory.EnumerateFiles(pathSegment, searchPattern))
                                {
                                    // add extension.
                                    for (int i = 0; i < pathExtSegments.Length; i++)
                                    {
                                        string fullPath = Path.Combine(pathSegment, $"{command}{pathExtSegments[i]}");
                                        if (string.Equals(file, fullPath, StringComparison.OrdinalIgnoreCase) && IsPathValid(fullPath, trace))
                                        {
                                            trace?.Info($"Location: '{fullPath}'");
                                            return fullPath;
                                        }
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                trace?.Info("Ignore UnauthorizedAccess exception during Which.");
                                trace?.Verbose(ex.ToString());
                            }
                        }
                    } else {
                        try
                        {
                            foreach (var file in Directory.EnumerateFiles(pathSegment, command))
                            {
                                if (IsPathValid(file, trace))
                                {
                                    trace?.Info($"Location: '{file}'");
                                    return file;
                                }
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            trace?.Info("Ignore UnauthorizedAccess exception during Which.");
                            trace?.Verbose(ex.ToString());
                        }
                    }
                }
            }

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                trace?.Info($"{command}: command not found. Make sure '{command}' is installed and its location included in the 'Path' environment variable.");
            }
            else
            {
                trace?.Info($"{command}: command not found. Make sure '{command}' is installed and its location included in the 'PATH' environment variable.");
            }
            if (require)
            {
                throw new FileNotFoundException(
                    message: $"{command}: command not found",
                    fileName: command);
            }

            return null;
        }

        // checks if the file is a symlink and if the symlink`s target exists.
        private static bool IsPathValid(string path, ITraceWriter trace = null)
        {
            var fileInfo = new FileInfo(path);
            var linkTargetFullPath = fileInfo.Directory?.FullName + Path.DirectorySeparatorChar + fileInfo.LinkTarget;
            if (fileInfo.LinkTarget == null ||
                File.Exists(linkTargetFullPath) ||
                File.Exists(fileInfo.LinkTarget))
            {
                return true;
            }
            trace?.Info($"the target '{fileInfo.LinkTarget}' of the symbolic link '{path}', does not exist");
            return false;
        }
    }
}
