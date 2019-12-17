using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.Common.ClientStorage
{
    /// <summary>
    /// Class providing access to local file storage, so data can persist across processes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)] // for internal use
    public class VssFileStorage : IVssClientStorage, IDisposable
    {
        private readonly string m_filePath;
        private readonly VssFileStorageReader m_reader;
        private readonly IVssClientStorageWriter m_writer;

        private const char c_defaultPathSeparator = '\\';
        private const bool c_defaultIgnoreCaseInPaths = false;

        /// <summary>
        /// The separator to use between the path segments of the storage keys.
        /// </summary>
        public char PathSeparator { get; }

        /// <summary>
        /// The StringComparer used to compare keys in the dictionary.
        /// </summary>
        public StringComparer PathComparer { get; }

        /// <summary>
        /// This constructor should remain private.  Use the factory method GetVssLocalFileStorage to ensure we only have one instance per file,
        /// which will reduce contention.
        /// </summary>
        /// <param name="filePath">This file path to store the settings.</param>
        /// <param name="pathSeparatorForKeys">The separator to use between the path segments of the storage keys.</param>
        /// <param name="ignoreCaseInPaths">If true the dictionary will use the OrdinalIgnoreCase StringComparer to compare keys.</param>
        private VssFileStorage(string filePath, char pathSeparatorForKeys = c_defaultPathSeparator, bool ignoreCaseInPaths = c_defaultIgnoreCaseInPaths) // This constructor should remain private.
        {
            PathSeparator = pathSeparatorForKeys;
            PathComparer = GetAppropriateStringComparer(ignoreCaseInPaths);
            m_filePath = filePath;
            m_reader = new VssFileStorageReader(m_filePath, pathSeparatorForKeys, PathComparer);
            m_writer = new VssFileStorageWriter(m_filePath, pathSeparatorForKeys, PathComparer);
        }

        public T ReadEntry<T>(string path)
        {
            return m_reader.ReadEntry<T>(path);
        }

        public T ReadEntry<T>(string path, T defaultValue)
        {
            return m_reader.ReadEntry<T>(path, defaultValue);
        }

        public IDictionary<string, T> ReadEntries<T>(string pathPrefix)
        {
            return m_reader.ReadEntries<T>(pathPrefix);
        }

        public void WriteEntries(IEnumerable<KeyValuePair<string, object>> entries)
        {
            m_writer.WriteEntries(entries);
            m_reader.NotifyChanged();
        }

        public void WriteEntry(string key, object value)
        {
            m_writer.WriteEntry(key, value);
            m_reader.NotifyChanged();
        }

        public void Dispose()
        {
            m_reader.Dispose();
        }

        public string PathKeyCombine(params string[] paths)
        {
            StringBuilder combinedPath = new StringBuilder();
            foreach (string segment in paths)
            {
                if (segment != null)
                {
                    string trimmedSegment = segment.TrimEnd(PathSeparator);
                    if (trimmedSegment.Length > 0)
                    {
                        if (combinedPath.Length > 0)
                        {
                            combinedPath.Append(PathSeparator);
                        }
                        combinedPath.Append(trimmedSegment);
                    }
                }
            }
            return combinedPath.ToString();
        }

        private static ConcurrentDictionary<string, VssFileStorage> s_storages = new ConcurrentDictionary<string, VssFileStorage>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Factory method to get a VssFileStorage instance ensuring that we don't have two instances for the same file.
        /// </summary>
        /// <param name="fullPath">The full path to the storage file.  Ensure that the path used is in an appropriately secure location for the data you are storing.</param>
        /// <param name="pathSeparatorForKeys">The separator to use between the path segments of the storage keys.</param>
        /// <param name="ignoreCaseInPaths">If true the dictionary will use the OrdinalIgnoreCase StringComparer to compare keys.</param>
        /// <returns></returns>
        public static IVssClientStorage GetVssLocalFileStorage(string fullPath, char pathSeparatorForKeys = c_defaultPathSeparator, bool ignoreCaseInPaths = c_defaultIgnoreCaseInPaths)
        {
            string normalizedFullPath = Path.GetFullPath(fullPath);
            VssFileStorage storage = s_storages.GetOrAdd(normalizedFullPath, (key) => new VssFileStorage(key, pathSeparatorForKeys, ignoreCaseInPaths));

            // we need to throw on mismatch if the cache contains a conflicting instance
            if (storage.PathSeparator != pathSeparatorForKeys)
            {
                throw new ArgumentException(CommonResources.ConflictingPathSeparatorForVssFileStorage(pathSeparatorForKeys, normalizedFullPath, storage.PathSeparator));
            }

            StringComparer pathComparer = GetAppropriateStringComparer(ignoreCaseInPaths);
            {
                if (storage.PathComparer != pathComparer)
                {
                    string caseSensitive = "Ordinal";
                    string caseInsensitive = "OrdinalIgnoreCase";
                    string requested = ignoreCaseInPaths ? caseInsensitive : caseSensitive;
                    string previous = ignoreCaseInPaths ? caseSensitive : caseInsensitive;
                    throw new ArgumentException(CommonResources.ConflictingStringComparerForVssFileStorage(requested, normalizedFullPath, previous));
                }
            }

#if DEBUG
            Debug.Assert(fullPath.Equals(storage.m_filePath), string.Format("The same storage file is being referenced with different casing.  This will cause issues when running in cross patform environments where the file system may be case sensitive.  {0} != {1}", storage.m_filePath, normalizedFullPath));
#endif
            return storage;
        }

        private static StringComparer GetAppropriateStringComparer(bool ignoreCase)
        {
            return ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }

        /// <summary>
        /// Gets an instance of a VssLocalFileStorage under the current user directory.
        /// </summary>
        /// <param name="pathSuffix">This pathSuffix will be combined at the end of the current user data directory for VSS to make a full path.  Something like: "%localappdata%\GitHub\ActionsService\[pathSuffix]"</param>
        /// <param name="storeByVssVersion">Adds the current product version as a path segment.  ...\GitHub\ActionsService\v[GeneratedVersionInfo.ProductVersion]\[pathSuffix]"</param>
        /// <param name="pathSeparatorForKeys">The separator to use between the path segments of the storage keys.</param>
        /// <param name="ignoreCaseInPaths">If true the dictionary will use the OrdinalIgnoreCase StringComparer to compare keys.</param>
        /// <returns></returns>
        public static IVssClientStorage GetCurrentUserVssFileStorage(string pathSuffix, bool storeByVssVersion, char pathSeparatorForKeys = c_defaultPathSeparator, bool ignoreCaseInPaths = c_defaultIgnoreCaseInPaths)
        {
            return GetVssLocalFileStorage(Path.Combine(storeByVssVersion ? ClientSettingsDirectoryByVersion : ClientSettingsDirectory, pathSuffix), pathSeparatorForKeys, ignoreCaseInPaths);
        }

        /// <summary>
        /// Directory containing the client settings files.
        ///
        /// This will look something like this:
        /// C:\Users\[user]\AppData\Local\GitHub\ActionsService\v[GeneratedVersionInfo.ProductVersion]
        /// </summary>
        internal static string ClientSettingsDirectoryByVersion
        {
            get
            {
                // We purposely do not cache this value. This value needs to change if
                // Windows Impersonation is being used.
                return Path.Combine(ClientSettingsDirectory, "v" + GeneratedVersionInfo.ProductVersion);
            }
        }

        /// <summary>
        /// Directory containing the client settings files.
        ///
        /// This will look something like this:
        /// C:\Users\[user]\AppData\Local\GitHub\ActionsService
        /// </summary>
        internal static string ClientSettingsDirectory
        {
            get
            {
                // We purposely do not cache this value. This value needs to change if 
                // Windows Impersonation is being used.

                // Check to see if we can find the user's local application data directory.
                string subDir = "GitHub\\ActionsService";
                string path = Environment.GetEnvironmentVariable("localappdata");
                SafeGetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(path))
                {
                    // If the user has never logged onto this box they will not have a local application data directory.
                    // Check to see if they have a roaming network directory that moves with them.
                    path = SafeGetFolderPath(Environment.SpecialFolder.ApplicationData);
                    if (string.IsNullOrEmpty(path))
                    {
                        // The user does not have a roaming network directory either. Just place the cache in the
                        // common area.
                        // If we are using the common dir, we might not have access to create a folder under "GitHub"
                        // so we just create a top level folder.
                        path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                        subDir = "GitHubActionsService";
                    }
                }

                Debug.Assert(path != null, "folder path cannot be null");
                return Path.Combine(path, subDir);
            }
        }

        /// <summary>
        /// Gets folder path and returns null in case the special folder in question doesn't exist (useful when the user has never logged on, which makes
        /// GetFolderPath throw)
        /// </summary>
        /// <param name="specialFolder">Folder to retrieve</param>
        /// <returns>Path if available, null othewise</returns>
        private static string SafeGetFolderPath(Environment.SpecialFolder specialFolder)
        {
            try
            {
                return Environment.GetFolderPath(specialFolder);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private class VssFileStorageReader : VssLocalFile, IVssClientStorageReader, IDisposable
        {
            private readonly string m_path;
            private Dictionary<string, JRaw> m_settings;

            private readonly FileSystemWatcher m_watcher;
            private readonly ReaderWriterLockSlim m_lock;
            private long m_completedRefreshId;
            private long m_outstandingRefreshId;

            public VssFileStorageReader(string fullPath, char pathSeparator, StringComparer comparer)
                : base(fullPath, pathSeparator, comparer)
            {
                m_path = fullPath;
                m_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                m_completedRefreshId = 0;
                m_outstandingRefreshId = 1;

                // Set up the file system watcher
                {
                    string directoryToWatch = Path.GetDirectoryName(m_path);

                    if (!Directory.Exists(directoryToWatch))
                    {
                        Directory.CreateDirectory(directoryToWatch);
                    }

                    m_watcher = new FileSystemWatcher(directoryToWatch, Path.GetFileName(m_path));
                    m_watcher.IncludeSubdirectories = false;
                    m_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                    m_watcher.Changed += OnCacheFileChanged;
                    m_watcher.EnableRaisingEvents = true;
                }
            }

            public T ReadEntry<T>(string path)
            {
                return ReadEntry<T>(path, default(T));
            }

            public T ReadEntry<T>(string path, T defaultValue)
            {
                path = NormalizePath(path);
                RefreshIfNeeded();

                Dictionary<string, JRaw> settings = m_settings;  // use a pointer to m_settings, incase m_settings gets set to a new instance during the operation
                JRaw value;
                if (settings.TryGetValue(path, out value) && value != null)
                {
                    return JsonConvert.DeserializeObject<T>(value.ToString());
                }
                return defaultValue;
            }

            public IDictionary<string, T> ReadEntries<T>(string pathPrefix)
            {
                string prefix = NormalizePath(pathPrefix, true);
                RefreshIfNeeded();
                Dictionary<string, JRaw> settings = m_settings;  // use a pointer to m_settings, incase m_settings gets set to a new instance during the operation
                Dictionary<string, T> matchingEntries = new Dictionary<string, T>();
                foreach (KeyValuePair<string, JRaw> kvp in settings.Where(kvp => kvp.Key == prefix || kvp.Key.StartsWith(prefix + PathSeparator)))
                {
                    try
                    {
                        matchingEntries[kvp.Key] = JsonConvert.DeserializeObject<T>(kvp.Value.ToString());
                    }
                    catch (JsonSerializationException) { }
                    catch (JsonReaderException) { }
                }
                return matchingEntries;
            }

            private void OnCacheFileChanged(object sender, FileSystemEventArgs e)
            {
                NotifyChanged();
            }

            public void Dispose()
            {
                m_watcher.Dispose();
            }

            public void NotifyChanged()
            {
                using (new ReadLockScope(m_lock))
                {
                    Interlocked.Increment(ref m_outstandingRefreshId);
                }
            }

            private void RefreshIfNeeded()
            {
                long requestedRefreshId;

                using (new ReadLockScope(m_lock))
                {
                    requestedRefreshId = Interlocked.Read(ref m_outstandingRefreshId);

                    if (m_completedRefreshId >= requestedRefreshId)
                    {
                        return;
                    }
                }

                Dictionary<string, JRaw> newSettings;
                using (GetNewMutexScope())
                {
                    if (m_completedRefreshId >= requestedRefreshId)
                    {
                        return;
                    }
                    newSettings = LoadFile();
                }

                using (new ReadLockScope(m_lock))
                {
                    if (m_completedRefreshId >= requestedRefreshId)
                    {
                        return;
                    }
                }

                using (new WriteLockScope(m_lock))
                {
                    if (m_completedRefreshId >= requestedRefreshId)
                    {
                        return;
                    }

                    m_completedRefreshId = requestedRefreshId;
                    m_settings = newSettings;
                }
            }

            private struct ReadLockScope : IDisposable
            {
                public ReadLockScope(ReaderWriterLockSlim @lock)
                {
                    m_lock = @lock;

                    m_lock.EnterReadLock();
                }

                public void Dispose()
                {
                    m_lock.ExitReadLock();
                }

                private readonly ReaderWriterLockSlim m_lock;
            }

            private struct WriteLockScope : IDisposable
            {
                public WriteLockScope(ReaderWriterLockSlim @lock)
                {
                    m_lock = @lock;
                    m_lock.EnterWriteLock();
                }

                public void Dispose()
                {
                    m_lock.ExitWriteLock();
                }

                private readonly ReaderWriterLockSlim m_lock;
            }
        }

        private class VssFileStorageWriter : VssLocalFile, IVssClientStorageWriter
        {
            public VssFileStorageWriter(string fullPath, char pathSeparator, StringComparer comparer)
                : base(fullPath, pathSeparator, comparer)
            {
            }

            public void WriteEntries(IEnumerable<KeyValuePair<string, object>> entries)
            {
                if (entries.Any())
                {
                    using (GetNewMutexScope())
                    {
                        bool changesMade = false;
                        Dictionary<string, JRaw> originalSettings = LoadFile();
                        Dictionary<string, JRaw> newSettings = new Dictionary<string, JRaw>(PathComparer);
                        if (originalSettings.Any())
                        {
                            originalSettings.Copy(newSettings);
                        }
                        foreach (KeyValuePair<string, object> kvp in entries)
                        {
                            string path = NormalizePath(kvp.Key);
                            if (kvp.Value != null)
                            {
                                JRaw jRawValue = new JRaw(JsonConvert.SerializeObject(kvp.Value));
                                if (!newSettings.ContainsKey(path) || !newSettings[path].Equals(jRawValue))
                                {
                                    newSettings[path] = jRawValue;
                                    changesMade = true;
                                }
                            }
                            else
                            {
                                if (newSettings.Remove(path))
                                {
                                    changesMade = true;
                                }
                            }
                        }
                        if (changesMade)
                        {
                            SaveFile(originalSettings, newSettings);
                        }
                    }
                }
            }

            public void WriteEntry(string path, object value)
            {
                WriteEntries(new KeyValuePair<string, object>[] { new KeyValuePair<string, object>(path, value) });
            }
        }

        private class VssLocalFile
        {
            private readonly string m_filePath;
            private readonly string m_bckUpFilePath;
            private readonly string m_emptyPathSegment;

            public VssLocalFile(string filePath, char pathSeparator, StringComparer comparer)
            {
                m_filePath = filePath;
                PathComparer = comparer;
                PathSeparator = pathSeparator;
                m_emptyPathSegment = new string(pathSeparator, 2);
                FileInfo fileInfo = new FileInfo(m_filePath);
                m_bckUpFilePath = Path.Combine(fileInfo.Directory.FullName, "~" + fileInfo.Name);
            }

            protected char PathSeparator { get; }

            protected string NormalizePath(string path, bool allowRootPath = false)
            {
                if (string.IsNullOrEmpty(path) || path[0] != PathSeparator || path.IndexOf(m_emptyPathSegment, StringComparison.Ordinal) >= 0 || (!allowRootPath && path.Length == 1))
                {
                    throw new ArgumentException(CommonResources.InvalidClientStoragePath(path, PathSeparator), "path");
                }
                if (path[path.Length - 1] == PathSeparator)
                {
                    path = path.Substring(0, path.Length - 1);
                }
                return path;
            }

            protected StringComparer PathComparer { get; }

            protected Dictionary<string, JRaw> LoadFile()
            {
                Dictionary<string, JRaw> settings = null;
                if (File.Exists(m_filePath))
                {
                    settings = LoadFile(m_filePath);
                }
                if ((settings == null || !settings.Any()) && File.Exists(m_bckUpFilePath))
                {
                    settings = LoadFile(m_bckUpFilePath);
                }
                return settings ?? new Dictionary<string, JRaw>(PathComparer);
            }

            private Dictionary<string, JRaw> LoadFile(string path)
            {
                Dictionary<string, JRaw> settings = new Dictionary<string, JRaw>(PathComparer);
                try
                {
                    string fileContent;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                    {
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            fileContent = sr.ReadToEnd();
                        }
                    }
                    IReadOnlyDictionary<string, JRaw> loadedSettings = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, JRaw>>(fileContent);
                    if (loadedSettings != null)
                    {
                        // Replay the settings into our dictionary one by one so that our uniqueness constraint
                        // isn't violated based on the StringComparer for this instance.
                        foreach (KeyValuePair<string, JRaw> setting in loadedSettings)
                        {
                            settings[setting.Key] = setting.Value;
                        }
                    }
                }
                catch (DirectoryNotFoundException) { }
                catch (FileNotFoundException) { }
                catch (JsonReaderException) { }
                catch (JsonSerializationException) { }
                catch (InvalidCastException) { }

                return settings;
            }

            protected void SaveFile(IDictionary<string, JRaw> originalSettings, IDictionary<string, JRaw> newSettings)
            {
                string newContent = JValue.Parse(JsonConvert.SerializeObject(newSettings)).ToString(Formatting.Indented);
                if (originalSettings.Any())
                {
                    // during testing, creating this backup provided reliability in the event of aborted threads, and
                    // crashed processes.  With this, I was not able to simulate a case where corruption happens, but there is no
                    // 100% gaurantee against corruption.
                    string originalContent = JValue.Parse(JsonConvert.SerializeObject(originalSettings)).ToString(Formatting.Indented);
                    SaveFile(m_bckUpFilePath, originalContent);
                }
                SaveFile(m_filePath, newContent);
                if (File.Exists(m_bckUpFilePath))
                {
                    File.Delete(m_bckUpFilePath);
                }
            }

            private void SaveFile(string path, string content)
            {
                bool success = false;
                int tries = 0;
                int retryDelayMilliseconds = 10;
                const int maxNumberOfRetries = 6;
                do
                {
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Delete))
                        {
                            using (var sw = new StreamWriter(fs, Encoding.UTF8))
                            {
                                sw.Write(content);
                            }
                        }
                        success = true;
                    }
                    catch (IOException)
                    {
                        if (++tries > maxNumberOfRetries)
                        {
                            throw;
                        }
                        Task.Delay(retryDelayMilliseconds).Wait();
                        retryDelayMilliseconds *= 2;
                    }
                }
                while (!success);
            }

            protected MutexScope GetNewMutexScope()
            {
                return new MutexScope(m_filePath.Replace(Path.DirectorySeparatorChar, '_'));
            }

            protected struct MutexScope : IDisposable
            {
                public MutexScope(string name)
                {
                    m_mutex = new Mutex(false, name);

                    try
                    {
                        if (!m_mutex.WaitOne(s_mutexTimeout))
                        {
                            throw new TimeoutException();
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // If this is thrown, then we hold the mutex.
                    }
                }

                public void Dispose()
                {
                    m_mutex.ReleaseMutex();
                }

                private readonly Mutex m_mutex;
                private static readonly TimeSpan s_mutexTimeout = TimeSpan.FromSeconds(10);
            }
        }
    }
}
