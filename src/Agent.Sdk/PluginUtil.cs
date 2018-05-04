using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.Win32;

namespace Agent.Sdk
{
    public class AgentCertificateSettings
    {
        public bool SkipServerCertificateValidation { get; set; }
        public string CACertificateFile { get; set; }
        public string ClientCertificateFile { get; set; }
        public string ClientCertificatePrivateKeyFile { get; set; }
        public string ClientCertificateArchiveFile { get; set; }
        public string ClientCertificatePassword { get; set; }
    }

    public class AgentWebProxySettings
    {
        public string ProxyAddress { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public List<string> ProxyBypassList { get; set; }

        private readonly List<Regex> _regExBypassList = new List<Regex>();
        private bool _initialized = false;
        public bool IsBypassed(Uri uri)
        {
            return string.IsNullOrEmpty(ProxyAddress) || uri.IsLoopback || IsMatchInBypassList(uri);
        }

        private bool IsMatchInBypassList(Uri input)
        {
            string matchUriString = input.IsDefaultPort ?
                input.Scheme + "://" + input.Host :
                input.Scheme + "://" + input.Host + ":" + input.Port.ToString();

            if (!_initialized)
            {
                InitializeBypassList();
            }

            foreach (Regex r in _regExBypassList)
            {
                if (r.IsMatch(matchUriString))
                {
                    return true;
                }
            }

            return false;
        }

        private void InitializeBypassList()
        {
            foreach (string bypass in ProxyBypassList)
            {
                Regex bypassRegex = new Regex(bypass, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ECMAScript);
                _regExBypassList.Add(bypassRegex);
            }

            _initialized = true;
        }
    }

    public static class PluginUtil
    {
        private static readonly object[] s_defaultFormatArgs = new object[] { null };
        private static Dictionary<string, object> s_locStrings;
        private static Lazy<JsonSerializerSettings> s_serializerSettings = new Lazy<JsonSerializerSettings>(() => new VssJsonMediaTypeFormatter().SerializerSettings);
        private static List<Version> _versions;

        static PluginUtil()
        {
#if OS_WINDOWS
            // By default, only Unicode encodings, ASCII, and code page 28591 are supported.
            // This line is required to support the full set of encodings that were included
            // in Full .NET prior to 4.6.
            //
            // For example, on an en-US box, this is required for loading the encoding for the
            // default console output code page '437'. Without loading the correct encoding for
            // code page IBM437, some characters cannot be translated correctly, e.g. write 'ç'
            // from powershell.exe.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        #region NetFrameworkUtil
        public static bool TestNetFrameworkVersion(AgentTaskPluginExecutionContext executionContext, Version minVersion)
        {
            PluginUtil.NotNull(minVersion, nameof(minVersion));
            InitVersions(executionContext);
            executionContext.Debug($"Testing for min NET Framework version: '{minVersion}'");
            return _versions.Any(x => x >= minVersion);
        }

        private static void InitVersions(AgentTaskPluginExecutionContext executionContext)
        {
            // See http://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx for details on how to detect framework versions
            // Also see http://support.microsoft.com/kb/318785

            if (_versions != null)
            {
                return;
            }

            var versions = new List<Version>();

            // Check for install root.
            string installRoot = GetHklmValue(executionContext, @"SOFTWARE\Microsoft\.NETFramework", "InstallRoot") as string;
            if (!string.IsNullOrEmpty(installRoot))
            {
                // Get the version sub key names.
                string ndpKeyName = @"SOFTWARE\Microsoft\NET Framework Setup\NDP";
                string[] versionSubKeyNames = GetHklmSubKeyNames(executionContext, ndpKeyName)
                    .Where(x => x.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                foreach (string versionSubKeyName in versionSubKeyNames)
                {
                    string versionKeyName = $@"{ndpKeyName}\{versionSubKeyName}";

                    // Test for the version value.
                    string version = GetHklmValue(executionContext, versionKeyName, "Version") as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        // Test for the install flag.
                        object install = GetHklmValue(executionContext, versionKeyName, "Install");
                        if (!(install is int) || (int)install != 1)
                        {
                            continue;
                        }

                        // Test for the install path.
                        string installPath = Path.Combine(installRoot, versionSubKeyName);
                        executionContext.Debug($"Testing directory: '{installPath}'");
                        if (!Directory.Exists(installPath))
                        {
                            continue;
                        }

                        // Parse the version from the sub key name.
                        Version versionObject;
                        if (!Version.TryParse(versionSubKeyName.Substring(1), out versionObject)) // skip over the leading "v".
                        {
                            executionContext.Debug($"Unable to parse version from sub key name: '{versionSubKeyName}'");
                            continue;
                        }

                        executionContext.Debug($"Found version: {versionObject}");
                        versions.Add(versionObject);
                        continue;
                    }

                    // Test if deprecated.
                    if (string.Equals(GetHklmValue(executionContext, versionKeyName, string.Empty) as string, "deprecated", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get the profile key names.
                    string[] profileKeyNames = GetHklmSubKeyNames(executionContext, versionKeyName)
                        .Select(x => $@"{versionKeyName}\{x}")
                        .ToArray();
                    foreach (string profileKeyName in profileKeyNames)
                    {
                        // Test for the version value.
                        version = GetHklmValue(executionContext, profileKeyName, "Version") as string;
                        if (string.IsNullOrEmpty(version))
                        {
                            continue;
                        }

                        // Test for the install flag.
                        object install = GetHklmValue(executionContext, profileKeyName, "Install");
                        if (!(install is int) || (int)install != 1)
                        {
                            continue;
                        }

                        // Test for the install path.
                        string installPath = (GetHklmValue(executionContext, profileKeyName, "InstallPath") as string ?? string.Empty)
                            .TrimEnd(Path.DirectorySeparatorChar);
                        if (string.IsNullOrEmpty(installPath))
                        {
                            continue;
                        }

                        // Determine the version string.
                        //
                        // Use a range since customer might install beta/preview .NET Framework.
                        string versionString = null;
                        object releaseObject = GetHklmValue(executionContext, profileKeyName, "Release");
                        if (releaseObject != null)
                        {
                            executionContext.Debug("Type is " + releaseObject.GetType().FullName);
                        }

                        if (releaseObject is int)
                        {
                            int release = (int)releaseObject;
                            if (release == 378389)
                            {
                                versionString = "4.5.0";
                            }
                            else if (release > 378389 && release <= 378758)
                            {
                                versionString = "4.5.1";
                            }
                            else if (release > 378758 && release <= 379893)
                            {
                                versionString = "4.5.2";
                            }
                            else if (release > 379893 && release <= 380995)
                            {
                                versionString = "4.5.3";
                            }
                            else if (release > 380995 && release <= 393297)
                            {
                                versionString = "4.6.0";
                            }
                            else if (release > 393297 && release <= 394271)
                            {
                                versionString = "4.6.1";
                            }
                            else if (release > 394271 && release <= 394806)
                            {
                                versionString = "4.6.2";
                            }
                            else if (release > 394806)
                            {
                                versionString = "4.7.0";
                            }
                            else
                            {
                                executionContext.Debug($"Release '{release}' did not fall into an expected range.");
                            }
                        }

                        if (string.IsNullOrEmpty(versionString))
                        {
                            continue;
                        }

                        executionContext.Debug($"Interpreted version: {versionString}");
                        versions.Add(new Version(versionString));
                    }
                }
            }

            executionContext.Debug($"Found {versions.Count} versions:");
            foreach (Version versionObject in versions)
            {
                executionContext.Debug($" {versionObject}");
            }

            Interlocked.CompareExchange(ref _versions, versions, null);
        }

        private static string[] GetHklmSubKeyNames(AgentTaskPluginExecutionContext executionContext, string keyName)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName);
            if (key == null)
            {
                executionContext.Debug($"Key name '{keyName}' is null.");
                return new string[0];
            }

            try
            {
                string[] subKeyNames = key.GetSubKeyNames() ?? new string[0];
                executionContext.Debug($"Key name '{keyName}' contains sub keys:");
                foreach (string subKeyName in subKeyNames)
                {
                    executionContext.Debug($" '{subKeyName}'");
                }

                return subKeyNames;
            }
            finally
            {
                key.Dispose();
            }
        }

        private static object GetHklmValue(AgentTaskPluginExecutionContext executionContext, string keyName, string valueName)
        {
            keyName = $@"HKEY_LOCAL_MACHINE\{keyName}";
            object value = Registry.GetValue(keyName, valueName, defaultValue: null);
            if (object.ReferenceEquals(value, null))
            {
                executionContext.Debug($"Key name '{keyName}', value name '{valueName}' is null.");
                return null;
            }

            executionContext.Debug($"Key name '{keyName}', value name '{valueName}': '{value}'");
            return value;
        }
        #endregion

        #region StringUtil
        public static T ConvertFromJson<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, s_serializerSettings.Value);
        }

        /// <summary>
        /// Convert String to boolean, valid true string: "1", "true", "$true", valid false string: "0", "false", "$false".
        /// </summary>
        /// <param name="value">value to convert.</param>
        /// <param name="defaultValue">default result when value is null or empty or not a valid true/false string.</param>
        /// <returns></returns>
        public static bool ConvertToBoolean(string value, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            switch (value.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "$true":
                    return true;
                case "0":
                case "false":
                case "$false":
                    return false;
                default:
                    return defaultValue;
            }
        }

        public static void EnsureRegisterEncodings()
        {
            // The static constructor should have registered the required encodings.
        }

        public static string Format(string format, params object[] args)
        {
            return Format(CultureInfo.InvariantCulture, format, args);
        }

        public static Encoding GetSystemEncoding()
        {
#if OS_WINDOWS
            // The static constructor should have registered the required encodings.
            // Code page 0 is equivalent to the current system default (i.e. CP_ACP).
            // E.g. code page 1252 on an en-US box.
            return Encoding.GetEncoding(0);
#else
            throw new NotSupportedException(nameof(GetSystemEncoding)); // Should never reach here.
#endif
        }

        // Do not combine the non-format overload with the format overload.
        public static string Loc(string locKey)
        {
            string locStr = locKey;
            try
            {
                EnsureLoaded();
                if (s_locStrings.ContainsKey(locKey))
                {
                    object item = s_locStrings[locKey];
                    if (item is string)
                    {
                        locStr = item as string;
                    }
                    else if (item is JArray)
                    {
                        string[] lines = (item as JArray).ToObject<string[]>();
                        var sb = new StringBuilder();
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                            {
                                sb.AppendLine();
                            }

                            sb.Append(lines[i]);
                        }

                        locStr = sb.ToString();
                    }
                }
                else
                {
                    locStr = Format("notFound:{0}", locKey);
                }
            }
            catch (Exception)
            {
                // loc strings shouldn't take down agent.  any failures returns loc key
            }

            return locStr;
        }

        // Do not combine the non-format overload with the format overload.
        public static string Loc(string locKey, params object[] args)
        {
            return Format(CultureInfo.CurrentCulture, Loc(locKey), args);
        }

        private static string Format(CultureInfo culture, string format, params object[] args)
        {
            try
            {
                // 1) Protect against argument null exception for the format parameter.
                // 2) Protect against argument null exception for the args parameter.
                // 3) Coalesce null or empty args with an array containing one null element.
                //    This protects against format exceptions where string.Format thinks
                //    that not enough arguments were supplied, even though the intended arg
                //    literally is null or an empty array.
                return string.Format(
                    culture,
                    format ?? string.Empty,
                    args == null || args.Length == 0 ? s_defaultFormatArgs : args);
            }
            catch (FormatException)
            {
                // TODO: Log that string format failed. Consider moving this into a context base class if that's the only place it's used. Then the current trace scope would be available as well.
                if (args != null)
                {
                    return string.Format(culture, "{0} {1}", format, string.Join(", ", args));
                }

                return format;
            }
        }

        private static void EnsureLoaded()
        {
            if (s_locStrings == null)
            {
                // Determine the list of resource files to load. The fallback "en-US" strings should always be
                // loaded into the dictionary first.
                string[] cultureNames;
                if (string.IsNullOrEmpty(CultureInfo.CurrentCulture.Name) || // Exclude InvariantCulture.
                    string.Equals(CultureInfo.CurrentCulture.Name, "en-US", StringComparison.Ordinal))
                {
                    cultureNames = new[] { "en-US" };
                }
                else
                {
                    cultureNames = new[] { "en-US", CultureInfo.CurrentCulture.Name };
                }

                // Initialize the dictionary.
                var locStrings = new Dictionary<string, object>();
                foreach (string cultureName in cultureNames)
                {
                    // Merge the strings from the file into the instance dictionary.
                    string file = Path.Combine(GetBinPath(), cultureName, "strings.json");
                    if (File.Exists(file))
                    {
                        foreach (KeyValuePair<string, object> pair in LoadObject<Dictionary<string, object>>(file))
                        {
                            locStrings[pair.Key] = pair.Value;
                        }
                    }
                }

                // Store the instance.
                s_locStrings = locStrings;
            }
        }
        #endregion

        #region ArgUtil
        public static void DirectoryExists(string directory, string name)
        {
            PluginUtil.NotNullOrEmpty(directory, name);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }
        }

        public static void Equal<T>(T expected, T actual, string name)
        {
            if (object.ReferenceEquals(expected, actual))
            {
                return;
            }

            if (object.ReferenceEquals(expected, null) ||
                !expected.Equals(actual))
            {
                throw new ArgumentOutOfRangeException(
                    paramName: name,
                    actualValue: actual,
                    message: $"{name} does not equal expected value. Expected '{expected}'. Actual '{actual}'.");
            }
        }

        public static void FileExists(string fileName, string name)
        {
            PluginUtil.NotNullOrEmpty(fileName, name);
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }
        }

        public static void NotNull(object value, string name)
        {
            if (object.ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void NotNullOrEmpty(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void NotEmpty(Guid value, string name)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void Null(object value, string name)
        {
            if (!object.ReferenceEquals(value, null))
            {
                throw new ArgumentException(message: $"{name} should be null.", paramName: name);
            }
        }
        #endregion

        #region UrlUtil
        public static Uri GetCredentialEmbeddedUrl(Uri baseUrl, string username, string password)
        {
            PluginUtil.NotNull(baseUrl, nameof(baseUrl));

            // return baseurl when there is no username and password
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                return baseUrl;
            }

            UriBuilder credUri = new UriBuilder(baseUrl);

            // ensure we have a username, uribuild will throw if username is empty but password is not.
            if (string.IsNullOrEmpty(username))
            {
                username = "emptyusername";
            }

            // escape chars in username for uri
            credUri.UserName = Uri.EscapeDataString(username);

            // escape chars in password for uri
            if (!string.IsNullOrEmpty(password))
            {
                credUri.Password = Uri.EscapeDataString(password);
            }

            return credUri.Uri;
        }
        #endregion

        #region ApiUtil
        public static VssConnection CreateConnection(Uri serverUri, VssCredentials credentials)
        {
            VssClientHttpRequestSettings settings = VssClientHttpRequestSettings.Default.Clone();

            int maxRetryRequest;
            if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_HTTP_RETRY") ?? string.Empty, out maxRetryRequest))
            {
                maxRetryRequest = 5;
            }

            // make sure MaxRetryRequest in range [5, 10]
            settings.MaxRetryRequest = Math.Min(Math.Max(maxRetryRequest, 5), 10);

            int httpRequestTimeoutSeconds;
            if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_HTTP_TIMEOUT") ?? string.Empty, out httpRequestTimeoutSeconds))
            {
                httpRequestTimeoutSeconds = 100;
            }

            // make sure httpRequestTimeoutSeconds in range [100, 1200]
            settings.SendTimeout = TimeSpan.FromSeconds(Math.Min(Math.Max(httpRequestTimeoutSeconds, 100), 1200));

            // Remove Invariant from the list of accepted languages.
            //
            // The constructor of VssHttpRequestSettings (base class of VssClientHttpRequestSettings) adds the current
            // UI culture to the list of accepted languages. The UI culture will be Invariant on OSX/Linux when the
            // LANG environment variable is not set when the program starts. If Invariant is in the list of accepted
            // languages, then "System.ArgumentException: The value cannot be null or empty." will be thrown when the
            // settings are applied to an HttpRequestMessage.
            settings.AcceptLanguages.Remove(CultureInfo.InvariantCulture);

            VssConnection connection = new VssConnection(serverUri, credentials, settings);
            return connection;
        }

        public static VssCredentials GetVssCredential(ServiceEndpoint serviceEndpoint)
        {
            NotNull(serviceEndpoint, nameof(serviceEndpoint));
            NotNull(serviceEndpoint.Authorization, nameof(serviceEndpoint.Authorization));
            NotNullOrEmpty(serviceEndpoint.Authorization.Scheme, nameof(serviceEndpoint.Authorization.Scheme));

            if (serviceEndpoint.Authorization.Parameters.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(serviceEndpoint));
            }

            VssCredentials credentials = null;
            string accessToken;
            if (serviceEndpoint.Authorization.Scheme == EndpointAuthorizationSchemes.OAuth &&
                serviceEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken))
            {
                credentials = new VssCredentials(null, new VssOAuthAccessTokenCredential(accessToken), CredentialPromptType.DoNotPrompt);
            }

            return credentials;
        }
        #endregion

        #region IOUtil
        public static T LoadObject<T>(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            return ConvertFromJson<T>(json);
        }

        public static string GetBinPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static void Delete(string path, CancellationToken cancellationToken)
        {
            DeleteDirectory(path, cancellationToken);
            DeleteFile(path);
        }

        public static void DeleteDirectory(string path, CancellationToken cancellationToken)
        {
            DeleteDirectory(path, contentsOnly: false, continueOnContentDeleteError: false, cancellationToken: cancellationToken);
        }

        public static void DeleteDirectory(string path, bool contentsOnly, bool continueOnContentDeleteError, CancellationToken cancellationToken)
        {
            PluginUtil.NotNullOrEmpty(path, nameof(path));
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                return;
            }

            if (!contentsOnly)
            {
                // Remove the readonly flag.
                RemoveReadOnly(directory);

                // Check if the directory is a reparse point.
                if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    // Delete the reparse point directory and short-circuit.
                    directory.Delete();
                    return;
                }
            }

            // Initialize a concurrent stack to store the directories. The directories
            // cannot be deleted until the files are deleted.
            var directories = new ConcurrentStack<DirectoryInfo>();

            if (!contentsOnly)
            {
                directories.Push(directory);
            }

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
                                // Remove the readonly attribute.
                                RemoveReadOnly(item);

                                // Check if the item is a file.
                                if (item is FileInfo)
                                {
                                    // Delete the file.
                                    item.Delete();
                                }
                                else
                                {
                                    // Check if the item is a directory reparse point.
                                    var subdirectory = item as DirectoryInfo;
                                    PluginUtil.NotNull(subdirectory, nameof(subdirectory));
                                    if (subdirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                                    {
                                        try
                                        {
                                            // Delete the reparse point.
                                            subdirectory.Delete();
                                        }
                                        catch (DirectoryNotFoundException)
                                        {
                                            // The target of the reparse point directory has been deleted.
                                            // Therefore the item is no longer a directory and is now a file.
                                            //
                                            // Deletion of reparse point directories happens in parallel. This case can occur
                                            // when reparse point directory FOO points to some other reparse point directory BAR,
                                            // and BAR is deleted after the DirectoryInfo for FOO has already been initialized.
                                            System.IO.File.Delete(subdirectory.FullName);
                                        }
                                    }
                                    else
                                    {
                                        // Store the directory.
                                        directories.Push(subdirectory);
                                    }
                                }

                                success = true;
                            }
                            catch (Exception) when (continueOnContentDeleteError)
                            {
                                // ignore any exception when continueOnContentDeleteError is true.
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
            PluginUtil.NotNullOrEmpty(path, nameof(path));
            var file = new FileInfo(path);
            if (file.Exists)
            {
                RemoveReadOnly(file);
                file.Delete();
            }
        }

        /// <summary>
        /// Recursively enumerates a directory without following directory reparse points.
        /// </summary>
        private static IEnumerable<FileSystemInfo> Enumerate(DirectoryInfo directory, CancellationTokenSource tokenSource)
        {
            PluginUtil.NotNull(directory, nameof(directory));
            PluginUtil.Equal(false, directory.Attributes.HasFlag(FileAttributes.ReparsePoint), nameof(directory.Attributes.HasFlag));

            // Push the directory onto the processing stack.
            var directories = new Stack<DirectoryInfo>(new[] { directory });
            while (directories.Count > 0)
            {
                // Pop the next directory.
                directory = directories.Pop();
                foreach (FileSystemInfo item in directory.GetFileSystemInfos())
                {
                    // Push non-reparse-point directories onto the processing stack.
                    directory = item as DirectoryInfo;
                    if (directory != null &&
                        !item.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        directories.Push(directory);
                    }

                    // Then yield the directory. Otherwise there is a race condition when this method attempts to initialize
                    // the Attributes and the caller is deleting the reparse point in parallel (FileNotFoundException).
                    yield return item;
                }
            }
        }

        private static void RemoveReadOnly(FileSystemInfo item)
        {
            PluginUtil.NotNull(item, nameof(item));
            if (item.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                item.Attributes = item.Attributes & ~FileAttributes.ReadOnly;
            }
        }
        #endregion

        #region VarUtil
        public static string PrependPath(string path, string currentPath)
        {
            PluginUtil.NotNullOrEmpty(path, nameof(path));
            if (string.IsNullOrEmpty(currentPath))
            {
                // Careful not to add a trailing separator if the PATH is empty.
                // On OSX/Linux, a trailing separator indicates that "current directory"
                // is added to the PATH, which is considered a security risk.
                return path;
            }

            return path + Path.PathSeparator + currentPath;
        }

        public static void PrependPath(string directory)
        {
            PluginUtil.DirectoryExists(directory, nameof(directory));

            // Build the new value.
            string currentPath = Environment.GetEnvironmentVariable("PATH");
            string path = PrependPath(directory, currentPath);

            // Update the PATH environment variable.
            Environment.SetEnvironmentVariable("PATH", path);
        }
        #endregion

        #region WhichUtil
        public static string Which(string command, bool require = false)
        {
            PluginUtil.NotNullOrEmpty(command, nameof(command));

#if OS_WINDOWS
            string path = Environment.GetEnvironmentVariable("Path");
#else
            string path = Environment.GetEnvironmentVariable("PATH");
#endif
            if (string.IsNullOrEmpty(path))
            {
                path = path ?? string.Empty;
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
                        searchPattern = $"{command}.*";
                        matches = Directory.GetFiles(pathSegment, searchPattern);
                        if (matches != null && matches.Length > 0)
                        {
                            // add extension.
                            for (int i = 0; i < pathExtSegments.Length; i++)
                            {
                                string fullPath = Path.Combine(pathSegment, $"{command}{pathExtSegments[i]}");
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

            if (require)
            {
                throw new FileNotFoundException(command);
            }

            return null;
        }
        #endregion
    }

    // The implementation of the process invoker does not hook up DataReceivedEvent and ErrorReceivedEvent of Process,
    // instead, we read both STDOUT and STDERR stream manually on seperate thread. 
    // The reason is we find a huge perf issue about process STDOUT/STDERR with those events. 
    public sealed class ProcessInvoker
    {
        private Process _proc;
        private Stopwatch _stopWatch;
        private int _asyncStreamReaderCount = 0;
        private bool _waitingOnStreams = false;
        private readonly AsyncManualResetEvent _outputProcessEvent = new AsyncManualResetEvent();
        private readonly TaskCompletionSource<bool> _processExitedCompletionSource = new TaskCompletionSource<bool>();
        private readonly ConcurrentQueue<string> _errorData = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _outputData = new ConcurrentQueue<string>();
        private readonly TimeSpan _sigintTimeout = TimeSpan.FromMilliseconds(7500);
        private readonly TimeSpan _sigtermTimeout = TimeSpan.FromMilliseconds(2500);
        private readonly AgentTaskPluginExecutionContext executionContext;

        private class AsyncManualResetEvent
        {
            private volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

            public Task WaitAsync() { return m_tcs.Task; }

            public void Set()
            {
                var tcs = m_tcs;
                Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                    tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                tcs.Task.Wait();
            }

            public void Reset()
            {
                while (true)
                {
                    var tcs = m_tcs;
                    if (!tcs.Task.IsCompleted ||
                        Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                        return;
                }
            }
        }

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public ProcessInvoker(AgentTaskPluginExecutionContext executionContext)
        {
            this.executionContext = executionContext;
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: null,
                cancellationToken: cancellationToken);
        }

        public async Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            CancellationToken cancellationToken)
        {
            PluginUtil.Null(_proc, nameof(_proc));
            PluginUtil.NotNullOrEmpty(fileName, nameof(fileName));

            executionContext.Debug("Starting process:");
            executionContext.Debug($"  File name: '{fileName}'");
            executionContext.Debug($"  Arguments: '{arguments}'");
            executionContext.Debug($"  Working directory: '{workingDirectory}'");
            executionContext.Debug($"  Require exit code zero: '{requireExitCodeZero}'");
            executionContext.Debug($"  Encoding web name: {outputEncoding?.WebName} ; code page: '{outputEncoding?.CodePage}'");

            _proc = new Process();
            _proc.StartInfo.FileName = fileName;
            _proc.StartInfo.Arguments = arguments;
            _proc.StartInfo.WorkingDirectory = workingDirectory;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.CreateNoWindow = true;
            _proc.StartInfo.RedirectStandardInput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.RedirectStandardOutput = true;

            // Ensure we process STDERR even the process exit event happen before we start read STDERR stream. 
            if (_proc.StartInfo.RedirectStandardError)
            {
                Interlocked.Increment(ref _asyncStreamReaderCount);
            }

            // Ensure we process STDOUT even the process exit event happen before we start read STDOUT stream.
            if (_proc.StartInfo.RedirectStandardOutput)
            {
                Interlocked.Increment(ref _asyncStreamReaderCount);
            }

#if OS_WINDOWS
            // If StandardErrorEncoding or StandardOutputEncoding is not specified the on the
            // ProcessStartInfo object, then .NET PInvokes to resolve the default console output
            // code page:
            //      [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
            //      public extern static uint GetConsoleOutputCP();
            PluginUtil.EnsureRegisterEncodings();
#endif
            if (outputEncoding != null)
            {
                _proc.StartInfo.StandardErrorEncoding = outputEncoding;
                _proc.StartInfo.StandardOutputEncoding = outputEncoding;
            }

            // Copy the environment variables.
            if (environment != null && environment.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in environment)
                {
                    _proc.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            // Set the TF_BUILD env variable.
            _proc.StartInfo.Environment["TFSBUILD"] = "True";

            // Hook up the events.
            _proc.EnableRaisingEvents = true;
            _proc.Exited += ProcessExitedHandler;

            // Start the process.
            _stopWatch = Stopwatch.StartNew();
            _proc.Start();

            if (_proc.StartInfo.RedirectStandardInput)
            {
                // Close the input stream. This is done to prevent commands from blocking the build waiting for input from the user.
                _proc.StandardInput.Close();
            }

            // Start the standard error notifications, if appropriate.
            if (_proc.StartInfo.RedirectStandardError)
            {
                StartReadStream(_proc.StandardError, _errorData);
            }

            // Start the standard output notifications, if appropriate.
            if (_proc.StartInfo.RedirectStandardOutput)
            {
                StartReadStream(_proc.StandardOutput, _outputData);
            }

            using (var registration = cancellationToken.Register(async () => await CancelAndKillProcessTree()))
            {
                executionContext.Debug($"Process started with process id {_proc.Id}, waiting for process exit.");
                while (true)
                {
                    Task outputSignal = _outputProcessEvent.WaitAsync();
                    var signaled = await Task.WhenAny(outputSignal, _processExitedCompletionSource.Task);

                    if (signaled == outputSignal)
                    {
                        ProcessOutput();
                    }
                    else
                    {
                        _stopWatch.Stop();
                        break;
                    }
                }

                // Just in case there was some pending output when the process shut down go ahead and check the
                // data buffers one last time before returning
                ProcessOutput();

                executionContext.Debug($"Finished process with exit code {_proc.ExitCode}, and elapsed time {_stopWatch.Elapsed}.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Wait for process to finish.
            if (_proc.ExitCode != 0 && requireExitCodeZero)
            {
                throw new ProcessExitCodeException(exitCode: _proc.ExitCode, fileName: fileName, arguments: arguments);
            }

            return _proc.ExitCode;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_proc != null)
                {
                    _proc.Dispose();
                    _proc = null;
                }
            }
        }

        private void ProcessOutput()
        {
            List<string> errorData = new List<string>();
            List<string> outputData = new List<string>();

            string errorLine;
            while (_errorData.TryDequeue(out errorLine))
            {
                errorData.Add(errorLine);
            }

            string outputLine;
            while (_outputData.TryDequeue(out outputLine))
            {
                outputData.Add(outputLine);
            }

            _outputProcessEvent.Reset();

            // Write the error lines.
            if (errorData != null && this.ErrorDataReceived != null)
            {
                foreach (string line in errorData)
                {
                    if (line != null)
                    {
                        this.ErrorDataReceived(this, new ProcessDataReceivedEventArgs(line));
                    }
                }
            }

            // Process the output lines.
            if (outputData != null && this.OutputDataReceived != null)
            {
                foreach (string line in outputData)
                {
                    if (line != null)
                    {
                        // The line is output from the process that was invoked.
                        this.OutputDataReceived(this, new ProcessDataReceivedEventArgs(line));
                    }
                }
            }
        }

        private async Task CancelAndKillProcessTree()
        {
            PluginUtil.NotNull(_proc, nameof(_proc));
            bool sigint_succeed = await SendSIGINT(_sigintTimeout);
            if (sigint_succeed)
            {
                executionContext.Debug("Process cancelled successfully through Ctrl+C/SIGINT.");
                return;
            }

            bool sigterm_succeed = await SendSIGTERM(_sigtermTimeout);
            if (sigterm_succeed)
            {
                executionContext.Debug("Process terminate successfully through Ctrl+Break/SIGTERM.");
                return;
            }

            executionContext.Debug("Kill entire process tree since both cancel and terminate signal has been ignored by the target process.");
            KillProcessTree();
        }

        private async Task<bool> SendSIGINT(TimeSpan timeout)
        {
#if OS_WINDOWS
            return await SendCtrlSignal(ConsoleCtrlEvent.CTRL_C, timeout);
#else
            return await SendSignal(Signals.SIGINT, timeout);
#endif
        }

        private async Task<bool> SendSIGTERM(TimeSpan timeout)
        {
#if OS_WINDOWS
            return await SendCtrlSignal(ConsoleCtrlEvent.CTRL_BREAK, timeout);
#else
            return await SendSignal(Signals.SIGTERM, timeout);
#endif
        }

        private void ProcessExitedHandler(object sender, EventArgs e)
        {
            if ((_proc.StartInfo.RedirectStandardError || _proc.StartInfo.RedirectStandardOutput) && _asyncStreamReaderCount != 0)
            {
                _waitingOnStreams = true;

                Task.Run(async () =>
                {
                    // Wait 5 seconds and then Cancel/Kill process tree
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    KillProcessTree();
                    _processExitedCompletionSource.TrySetResult(true);
                });
            }
            else
            {
                _processExitedCompletionSource.TrySetResult(true);
            }
        }

        private void StartReadStream(StreamReader reader, ConcurrentQueue<string> dataBuffer)
        {
            Task.Run(() =>
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line != null)
                    {
                        dataBuffer.Enqueue(line);
                        _outputProcessEvent.Set();
                    }
                }

                if (Interlocked.Decrement(ref _asyncStreamReaderCount) == 0 && _waitingOnStreams)
                {
                    _processExitedCompletionSource.TrySetResult(true);
                }
            });
        }

        private void KillProcessTree()
        {
#if OS_WINDOWS
            WindowsKillProcessTree();
#else
            NixKillProcessTree();
#endif
        }

#if OS_WINDOWS
        private async Task<bool> SendCtrlSignal(ConsoleCtrlEvent signal, TimeSpan timeout)
        {
            executionContext.Debug($"Sending {signal} to process {_proc.Id}.");
            ConsoleCtrlDelegate ctrlEventHandler = new ConsoleCtrlDelegate(ConsoleCtrlHandler);
            try
            {
                if (!FreeConsole())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!AttachConsole(_proc.Id))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!SetConsoleCtrlHandler(ctrlEventHandler, true))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!GenerateConsoleCtrlEvent(signal, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                executionContext.Debug($"Successfully send {signal} to process {_proc.Id}.");
                executionContext.Debug($"Waiting for process exit or {timeout.TotalSeconds} seconds after {signal} signal fired.");
                var completedTask = await Task.WhenAny(Task.Delay(timeout), _processExitedCompletionSource.Task);
                if (completedTask == _processExitedCompletionSource.Task)
                {
                    executionContext.Debug("Process exit successfully.");
                    return true;
                }
                else
                {
                    executionContext.Debug($"Process did not honor {signal} signal within {timeout.TotalSeconds} seconds.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                executionContext.Debug($"{signal} signal doesn't fire successfully.");
                executionContext.Debug($"Catch exception during send {signal} event to process {_proc.Id}");
                executionContext.Debug(ex.ToString());
                return false;
            }
            finally
            {
                FreeConsole();
                SetConsoleCtrlHandler(ctrlEventHandler, false);
            }
        }

        private bool ConsoleCtrlHandler(ConsoleCtrlEvent ctrlType)
        {
            switch (ctrlType)
            {
                case ConsoleCtrlEvent.CTRL_C:
                    executionContext.Debug($"Ignore Ctrl+C to current process.");
                    // We return True, so the default Ctrl handler will not take action.
                    return true;
                case ConsoleCtrlEvent.CTRL_BREAK:
                    executionContext.Debug($"Ignore Ctrl+Break to current process.");
                    // We return True, so the default Ctrl handler will not take action.
                    return true;
            }

            // If the function handles the control signal, it should return TRUE. 
            // If it returns FALSE, the next handler function in the list of handlers for this process is used.
            return false;
        }

        private void WindowsKillProcessTree()
        {
            Dictionary<int, int> processRelationship = new Dictionary<int, int>();
            executionContext.Debug($"Scan all processes to find relationship between all processes.");
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if (!proc.SafeHandle.IsInvalid)
                    {
                        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                        int returnLength = 0;
                        int queryResult = NtQueryInformationProcess(proc.SafeHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), ref returnLength);
                        if (queryResult == 0) // == 0 is OK
                        {
                            executionContext.Debug($"Process: {proc.Id} is child process of {pbi.InheritedFromUniqueProcessId}.");
                            processRelationship[proc.Id] = (int)pbi.InheritedFromUniqueProcessId;
                        }
                        else
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore all exceptions, since KillProcessTree is best effort.
                    executionContext.Debug("Ignore any catched exception during detecting process relationship.");
                    executionContext.Debug(ex.ToString());
                }
            }

            executionContext.Debug($"Start killing process tree of process '{_proc.Id}'.");
            Stack<ProcessTerminationInfo> processesNeedtoKill = new Stack<ProcessTerminationInfo>();
            processesNeedtoKill.Push(new ProcessTerminationInfo(_proc.Id, false));
            while (processesNeedtoKill.Count() > 0)
            {
                ProcessTerminationInfo procInfo = processesNeedtoKill.Pop();
                List<int> childProcessesIds = new List<int>();
                if (!procInfo.ChildPidExpanded)
                {
                    executionContext.Debug($"Find all child processes of process '{procInfo.Pid}'.");
                    childProcessesIds = processRelationship.Where(p => p.Value == procInfo.Pid).Select(k => k.Key).ToList();
                }

                if (childProcessesIds.Count > 0)
                {
                    executionContext.Debug($"Need kill all child processes trees before kill process '{procInfo.Pid}'.");
                    processesNeedtoKill.Push(new ProcessTerminationInfo(procInfo.Pid, true));
                    foreach (var childPid in childProcessesIds)
                    {
                        executionContext.Debug($"Child process '{childPid}' needs be killed first.");
                        processesNeedtoKill.Push(new ProcessTerminationInfo(childPid, false));
                    }
                }
                else
                {
                    executionContext.Debug($"Kill process '{procInfo.Pid}'.");
                    try
                    {
                        Process leafProcess = Process.GetProcessById(procInfo.Pid);
                        try
                        {
                            leafProcess.Kill();
                        }
                        catch (InvalidOperationException ex)
                        {
                            // The process has already exited
                            executionContext.Debug("Ignore InvalidOperationException during Process.Kill().");
                            executionContext.Debug(ex.ToString());
                        }
                        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
                        {
                            // The associated process could not be terminated
                            // The process is terminating
                            // NativeErrorCode 5 means Access Denied
                            executionContext.Debug("Ignore Win32Exception with NativeErrorCode 5 during Process.Kill().");
                            executionContext.Debug(ex.ToString());
                        }
                        catch (Exception ex)
                        {
                            // Ignore any additional exception
                            executionContext.Debug("Ignore additional exceptions during Process.Kill().");
                            executionContext.Debug(ex.ToString());
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        // process already gone, nothing needs killed.
                        executionContext.Debug("Ignore ArgumentException during Process.GetProcessById().");
                        executionContext.Debug(ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        // Ignore any additional exception
                        executionContext.Debug("Ignore additional exceptions during Process.GetProcessById().");
                        executionContext.Debug(ex.ToString());
                    }
                }
            }
        }

        private class ProcessTerminationInfo
        {
            public ProcessTerminationInfo(int pid, bool expanded)
            {
                Pid = pid;
                ChildPidExpanded = expanded;
            }

            public int Pid { get; }
            public bool ChildPidExpanded { get; }
        }

        private enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1
        }

        private enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public long ExitStatus;
            public long PebBaseAddress;
            public long AffinityMask;
            public long BasePriority;
            public long UniqueProcessId;
            public long InheritedFromUniqueProcessId;
        };


        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, ref int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        // Delegate type to be used as the Handler Routine for SetConsoleCtrlHandler
        private delegate Boolean ConsoleCtrlDelegate(ConsoleCtrlEvent CtrlType);
#else
        private async Task<bool> SendSignal(Signals signal, TimeSpan timeout)
        {
            executionContext.Debug($"Sending {signal} to process {_proc.Id}.");
            int errorCode = kill(_proc.Id, (int)signal);
            if (errorCode != 0)
            {
                executionContext.Debug($"{signal} signal doesn't fire successfully.");
                executionContext.Debug($"Error code: {errorCode}.");
                return false;
            }

            executionContext.Debug($"Successfully send {signal} to process {_proc.Id}.");
            executionContext.Debug($"Waiting for process exit or {timeout.TotalSeconds} seconds after {signal} signal fired.");
            var completedTask = await Task.WhenAny(Task.Delay(timeout), _processExitedCompletionSource.Task);
            if (completedTask == _processExitedCompletionSource.Task)
            {
                executionContext.Debug("Process exit successfully.");
                return true;
            }
            else
            {
                executionContext.Debug($"Process did not honor {signal} signal within {timeout.TotalSeconds} seconds.");
                return false;
            }
        }

        private void NixKillProcessTree()
        {
            try
            {
                if (!_proc.HasExited)
                {
                    _proc.Kill();
                }
            }
            catch (InvalidOperationException ex)
            {
                executionContext.Debug("Ignore InvalidOperationException during Process.Kill().");
                executionContext.Debug(ex.ToString());
            }
        }

        private enum Signals : int
        {
            SIGINT = 2,
            SIGTERM = 15
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);
#endif
    }

    public sealed class ProcessExitCodeException : Exception
    {
        public int ExitCode { get; private set; }

        public ProcessExitCodeException(int exitCode, string fileName, string arguments)
            : base($"ProcessExitCode {exitCode}, {fileName}, {arguments}")
        {
            ExitCode = exitCode;
        }
    }

    public sealed class ProcessDataReceivedEventArgs : EventArgs
    {
        public ProcessDataReceivedEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}
