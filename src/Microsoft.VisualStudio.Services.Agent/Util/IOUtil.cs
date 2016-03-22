using Microsoft.VisualStudio.Services.Agent;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class IOUtil
    {
        public static string ExeExtension
        {
            get
            {
#if OS_WINDOWS
                return ".exe";
#else
                return string.Empty;
#endif
            }
        }

        public static void SaveObject(Object obj, string path)
        {
            string json = JsonConvert.SerializeObject(
                obj,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(path, json);
        }

        public static T LoadObject<T>(string path)
        {
            string json = File.ReadAllText(path);
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }

        public static string GetBinPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static string GetDiagPath()
        {
            return Path.Combine(
                Path.GetDirectoryName(GetBinPath()),
                Constants.Path.DiagDirectory);
        }

        public static string GetExternalsPath()
        {
            return Path.Combine(
                GetRootPath(),
                Constants.Path.ExternalsDirectory);
        }

        public static string GetRootPath()
        {
            return new DirectoryInfo(GetBinPath()).Parent.FullName;
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(GetRootPath(), ".Agent");
        }

        public static string GetCredFilePath()
        {
            return Path.Combine(GetRootPath(), ".Credentials");
        }

        public static string GetWorkPath(IHostContext hostContext)
        {
            var configurationStore = hostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configurationStore.GetSettings();
            return Path.Combine(
                Path.GetDirectoryName(GetBinPath()),
                settings.WorkFolder);
        }

        public static string GetTasksPath(IHostContext hostContext)
        {
            return Path.Combine(
                GetWorkPath(hostContext),
                Constants.Path.TasksDirectory);
        }

        public static void DeleteDirectory(string path, CancellationToken cancellationToken)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                return;
            }

            // Initialize a concurrent stack to store the directories. The directories
            // cannot be deleted until the files are deleted.
            var directories = new ConcurrentStack<DirectoryInfo>();

            // Remove the readonly flag and store the root directory.
            RemoveReadOnly(directory);
            directories.Push(directory);

            // Create a new token source for the parallel query. The parallel query should be
            // canceled after the first error is encountered. Otherwise the number of exceptions
            // could get out of control for a large directory with access denied on every file.
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Delete all files and store all subdirectories.
                directory
                    .EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                    .AsParallel()
                    .WithCancellation(tokenSource.Token)
                    .ForAll((FileSystemInfo item) =>
                    {
                        bool success = false;
                        try
                        {
                            // Check if the item is a file.
                            var file = item as FileInfo;
                            if (file != null)
                            {
                                // Delete the file.
                                RemoveReadOnly(file);
                                file.Delete();
                                success = true;
                                return;
                            }

                            // The item is a directory.
                            var subdirectory = item as DirectoryInfo;
                            ArgUtil.NotNull(subdirectory, nameof(subdirectory));

                            // Remove the readonly attribute and store the subdirectory.
                            RemoveReadOnly(subdirectory);
                            directories.Push(subdirectory);
                            success = true;
                        }
                        finally
                        {
                            if (!success)
                            {
                                tokenSource.Cancel(); // Cancel is thread-safe.
                            }
                        }
                    });
            }

            // Delete the directories.
            foreach (DirectoryInfo dir in directories.OrderByDescending(x => x.FullName.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                dir.Delete();
            }
        }

        public static string Which(String command)
        {
            ArgUtil.NotNullOrEmpty(command, nameof(command));

#if OS_WINDOWS
            string path = Environment.GetEnvironmentVariable("Path");
#else
            string path = Environment.GetEnvironmentVariable("PATH");
#endif
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

#if OS_WINDOWS
            char pathSep = ';';
#else
            char pathSep = ':';
#endif

            string[] pathSegments = path.Split(new Char[] { pathSep }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pathSegments.Length; i++)
            {
                pathSegments[i] = Environment.ExpandEnvironmentVariables(pathSegments[i]);
            }

            foreach (string pathSegment in pathSegments)
            {
                if (!string.IsNullOrEmpty(pathSegment) && Directory.Exists(pathSegment))
                {
                    string[] matches;
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
                        matches = Directory.GetFiles(pathSegment, command);
                        if (matches != null && matches.Length > 0)
                        {
                            return matches.First();
                        }
                    }
                    else
                    {
                        string searchPattern;
                        searchPattern = StringUtil.Format($"{command}.*");
                        matches = Directory.GetFiles(pathSegment, searchPattern);
                        if (matches != null && matches.Length > 0)
                        {
                            // add extension.
                            for (int i = 0; i < pathExtSegments.Length; i++)
                            {
                                string fullPath = Path.Combine(pathSegment, StringUtil.Format($"{command}{pathExtSegments[i]}"));
                                if (matches.Any(p => p.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
                                {
                                    return fullPath;
                                }
                            }
                        }
                    }
#else
                    matches = Directory.GetFiles(pathSegment, command);
                    if (matches != null && matches.Length > 0)
                    {
                        return matches.First();
                    }
#endif
                }
            }

            return null;
        }

        private static void RemoveReadOnly(FileSystemInfo item)
        {
            ArgUtil.NotNull(item, nameof(item));
            if (item.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                item.Attributes = item.Attributes & ~FileAttributes.ReadOnly;
            }
        }
    }
}