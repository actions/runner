using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class IOUtil
    {
        private static Lazy<JsonSerializerSettings> s_serializerSettings = new Lazy<JsonSerializerSettings>(() => new VssJsonMediaTypeFormatter().SerializerSettings);

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

        public static String ToString(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, s_serializerSettings.Value);
        }

        public static T FromString<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, s_serializerSettings.Value);
        }

        public static void SaveObject(object obj, string path)
        {
            File.WriteAllText(path, ToString(obj), Encoding.UTF8);
        }

        public static T LoadObject<T>(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            return FromString<T>(json);
        }

        public static string GetBinPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static string GetBinPathHash()
        {
            string hashString = GetBinPath().ToLowerInvariant();
            using (SHA256 sha256hash = SHA256.Create())
            {
                byte[] data = sha256hash.ComputeHash(Encoding.UTF8.GetBytes(hashString));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                string hash = sBuilder.ToString();
                return hash;
            }
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
            return Path.Combine(GetRootPath(), ".agent");
        }

        public static string GetCredFilePath()
        {
            return Path.Combine(GetRootPath(), ".credentials");
        }

        public static string GetServiceConfigFilePath()
        {
            return Path.Combine(GetRootPath(), ".service");
        }

        public static string GetRSACredFilePath()
        {
            return Path.Combine(GetRootPath(), ".credentials_rsaparams");
        }

        public static string GetProxyConfigFilePath()
        {
            return Path.Combine(GetRootPath(), ".proxy");
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

        public static void Delete(string path, CancellationToken cancellationToken)
        {
            DeleteDirectory(path, cancellationToken);
            DeleteFile(path);
        }

        public static string GetUpdatePath(IHostContext hostContext)
        {
            return Path.Combine(
                GetWorkPath(hostContext),
                Constants.Path.UpdateDirectory);
        }

        public static void DeleteDirectory(string path, CancellationToken cancellationToken)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                return;
            }

            // Remove the readonly flag.
            RemoveReadOnly(directory);

            // Check if the directory is a reparse point.
            if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                // Delete the reparse point directory and short-circuit.
                directory.Delete();
                return;
            }

            // Initialize a concurrent stack to store the directories. The directories
            // cannot be deleted until the files are deleted.
            var directories = new ConcurrentStack<DirectoryInfo>();
            directories.Push(directory);

            // Create a new token source for the parallel query. The parallel query should be
            // canceled after the first error is encountered. Otherwise the number of exceptions
            // could get out of control for a large directory with access denied on every file.
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    // Recursively delete all files and store all subdirectories.
                    Enumerate(directory, tokenSource)
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
                catch (Exception)
                {
                    tokenSource.Cancel();
                    throw;
                }
            }

            // Delete the directories.
            foreach (DirectoryInfo dir in directories.OrderByDescending(x => x.FullName.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                dir.Delete();
            }
        }

        public static void DeleteFile(string path)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            var file = new FileInfo(path);
            if (file.Exists)
            {
                RemoveReadOnly(file);
                file.Delete();
            }
        }

        /// <summary>
        /// Given a path and directory, return the path relative to the directory.  If the path is not
        /// under the directory the path is returned un modified.  Examples:
        /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src") -> @"project\foo.cpp"
        /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\specs") -> @"d:\src\project\foo.cpp"
        /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\proj") -> @"d:\src\project\foo.cpp"
        /// </summary>
        /// <remarks>Safe for remote paths.  Does not access the local disk.</remarks>
        /// <param name="path">Path to make relative.</param>
        /// <param name="folder">Folder to make it relative to.</param>
        /// <returns>Relative path.</returns>
        public static string MakeRelative(string path, string folder)
        {
            ArgUtil.NotNullOrEmpty(path, nameof(path));
            ArgUtil.NotNull(folder, nameof(folder));

            // Replace all Path.AltDirectorySeparatorChar with Path.DirectorySeparatorChar from both inputs
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            folder = folder.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Check if the dir is a prefix of the path (if not, it isn't relative at all).
            if (!path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            // Dir is a prefix of the path, if they are the same length then the relative path is empty.
            if (path.Length == folder.Length)
            {
                return string.Empty;
            }

            // If the dir ended in a '\\' (like d:\) or '/' (like user/bin/)  then we have a relative path.
            if (folder.Length > 0 && folder[folder.Length - 1] == Path.DirectorySeparatorChar)
            {
                return path.Substring(folder.Length);
            }
            // The next character needs to be a '\\' or they aren't really relative.
            else if (path[folder.Length] == Path.DirectorySeparatorChar)
            {
                return path.Substring(folder.Length + 1);
            }
            else
            {
                return path;
            }
        }

        public static void CopyDirectory(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirectory);

            // If the source directory does not exist, throw an exception.
            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"{sourceDirectory}");
            }

            Directory.CreateDirectory(targetDirectory);

            // Get the file contents of the directory to copy.
            foreach (FileInfo file in sourceDir.GetFiles() ?? new FileInfo[0])
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Create the path to the new copy of the file.
                string targetFilePath = Path.Combine(targetDirectory, file.Name);
                // Copy the file.
                file.CopyTo(targetFilePath, true);
            }

            DirectoryInfo[] subDirs = sourceDir.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs ?? new DirectoryInfo[0])
            {
                // Create the subdirectory.
                string targetDirectoryPath = Path.Combine(targetDirectory, subDir.Name);
                // Copy the subdirectories.
                CopyDirectory(subDir.FullName, targetDirectoryPath, cancellationToken);
            }
        }

        /// <summary>
        /// Recursively enumerates a directory without following directory reparse points.
        /// </summary>
        private static IEnumerable<FileSystemInfo> Enumerate(DirectoryInfo directory, CancellationTokenSource tokenSource)
        {
            ArgUtil.NotNull(directory, nameof(directory));
            ArgUtil.Equal(false, directory.Attributes.HasFlag(FileAttributes.ReparsePoint), nameof(directory.Attributes.HasFlag));

            // Push the directory onto the processing stack.
            var directories = new Stack<DirectoryInfo>(new[] { directory });
            while (directories.Count > 0)
            {
                // Pop the next directory.
                directory = directories.Pop();
                foreach (FileSystemInfo item in directory.GetFileSystemInfos())
                {
                    yield return item;

                    // Push non-reparse-point directories onto the processing stack.
                    directory = item as DirectoryInfo;
                    if (directory != null &&
                        !item.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        directories.Push(directory);
                    }
                }
            }
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