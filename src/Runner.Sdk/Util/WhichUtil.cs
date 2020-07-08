using System;
using System.IO;
using System.Linq;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Sdk
{
    public static class WhichUtil
    {
        public static string Which(string command, bool require = false, ITraceWriter trace = null, string prependPath = null)
        {
            ArgUtil.NotNullOrEmpty(command, nameof(command));
            trace?.Info($"Which: '{command}'");
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
#if OS_WINDOWS
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
                            matches = Directory.GetFiles(pathSegment, searchPattern);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            trace?.Info("Ignore UnauthorizedAccess exception during Which.");
                            trace?.Verbose(ex.ToString());
                        }

                        if (matches != null && matches.Length > 0)
                        {
                            // add extension.
                            for (int i = 0; i < pathExtSegments.Length; i++)
                            {
                                string fullPath = Path.Combine(pathSegment, $"{command}{pathExtSegments[i]}");
                                if (matches.Any(p => p.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
                                {
                                    trace?.Info($"Location: '{fullPath}'");
                                    return fullPath;
                                }
                            }
                        }
                    }
#else
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
#endif
                }
            }

            trace?.Info("Not found.");
            if (require)
            {
                throw new FileNotFoundException(
                    message: $"File not found: '{command}'",
                    fileName: command);
            }

            return null;
        }
    }
}
