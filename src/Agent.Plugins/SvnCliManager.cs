using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Plugins.Repository
{
    public class SvnCliManager
    {
        /// <summary>
        /// Initializes svn command path and execution environment
        /// </summary>
        /// <param name="context">The build commands' execution context</param>
        /// <param name="endpoint">The Subversion server endpoint providing URL, username/password, and untrasted certs acceptace information</param>
        /// <param name="cancellationToken">The cancellation token used to stop svn command execution</param>
        public void Init(
            AgentTaskPluginExecutionContext context,
            Pipelines.RepositoryResource repository,
            CancellationToken cancellationToken)
        {
            // Validation.
            PluginUtil.NotNull(context, nameof(context));
            PluginUtil.NotNull(repository, nameof(repository));
            PluginUtil.NotNull(cancellationToken, nameof(cancellationToken));

            PluginUtil.NotNull(repository.Url, nameof(repository.Url));
            PluginUtil.Equal(true, repository.Url.IsAbsoluteUri, nameof(repository.Url.IsAbsoluteUri));

            PluginUtil.NotNull(repository.Endpoint, nameof(repository.Endpoint));
            ServiceEndpoint endpoint = context.Endpoints.Single(x => x.Id == repository.Endpoint.Id);
            PluginUtil.NotNull(endpoint.Data, nameof(endpoint.Data));
            PluginUtil.NotNull(endpoint.Authorization, nameof(endpoint.Authorization));
            PluginUtil.NotNull(endpoint.Authorization.Parameters, nameof(endpoint.Authorization.Parameters));
            PluginUtil.Equal(EndpointAuthorizationSchemes.UsernamePassword, endpoint.Authorization.Scheme, nameof(endpoint.Authorization.Scheme));

            _context = context;
            _repository = repository;
            _cancellationToken = cancellationToken;

            // Find svn in %Path%
            string svnPath = PluginUtil.Which("svn");

            if (string.IsNullOrEmpty(svnPath))
            {
                throw new Exception(PluginUtil.Loc("SvnNotInstalled"));
            }
            else
            {
                _context.Debug($"Found svn installation path: {svnPath}.");
                _svn = svnPath;
            }

            // External providers may need basic auth or tokens
            endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.Username, out _username);
            endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.Password, out _password);

            _acceptUntrusted = PluginUtil.ConvertToBoolean(repository.Properties.Get<string>(EndpointData.SvnAcceptUntrustedCertificates));
        }

        /// <summary>
        /// Detects old mappings (if any) and refreshes the SVN working copies to match the new mappings.
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="distinctMappings"></param>
        /// <param name="cleanRepository"></param>
        /// <param name="sourceBranch"></param>
        /// <param name="revision"></param>
        /// <returns></returns>
        public async Task<string> UpdateWorkspace(
            string rootPath,
            Dictionary<string, SvnMappingDetails> distinctMappings,
            bool cleanRepository,
            string sourceBranch,
            string revision)
        {
            if (cleanRepository)
            {
                // A clean build has been requested
                PluginUtil.DeleteDirectory(rootPath, _cancellationToken);
                Directory.CreateDirectory(rootPath);
            }

            Dictionary<string, Uri> oldMappings = await GetOldMappings(rootPath);
            _context.Debug($"oldMappings.Count: {oldMappings.Count}");
            oldMappings.ToList().ForEach(p => _context.Debug($"   [{p.Key}] {p.Value}"));

            Dictionary<string, SvnMappingDetails> newMappings = BuildNewMappings(rootPath, sourceBranch, distinctMappings);
            _context.Debug($"newMappings.Count: {newMappings.Count}");
            newMappings.ToList().ForEach(p => _context.Debug($"    [{p.Key}] ServerPath: {p.Value.ServerPath}, LocalPath: {p.Value.LocalPath}, Depth: {p.Value.Depth}, Revision: {p.Value.Revision}, IgnoreExternals: {p.Value.IgnoreExternals}"));

            CleanUpSvnWorkspace(oldMappings, newMappings);

            long maxRevision = 0;

            foreach (SvnMappingDetails mapping in newMappings.Values)
            {
                long mappingRevision = await GetLatestRevisionAsync(mapping.ServerPath, revision);
                if (mappingRevision > maxRevision)
                {
                    maxRevision = mappingRevision;
                }
            }

            await UpdateToRevisionAsync(oldMappings, newMappings, maxRevision);

            return maxRevision > 0 ? maxRevision.ToString() : "HEAD";
        }

        private async Task<Dictionary<string, Uri>> GetOldMappings(string rootPath)
        {
            if (File.Exists(rootPath))
            {
                throw new Exception(PluginUtil.Loc("SvnFileAlreadyExists", rootPath));
            }

            Dictionary<string, Uri> mappings = new Dictionary<string, Uri>();

            if (Directory.Exists(rootPath))
            {
                foreach (string workingDirectoryPath in GetSvnWorkingCopyPaths(rootPath))
                {
                    Uri url = await GetRootUrlAsync(workingDirectoryPath);

                    if (url != null)
                    {
                        mappings.Add(workingDirectoryPath, url);
                    }
                }
            }

            return mappings;
        }

        private List<string> GetSvnWorkingCopyPaths(string rootPath)
        {
            if (Directory.Exists(Path.Combine(rootPath, ".svn")))
            {
                return new List<string>() { rootPath };
            }
            else
            {
                ConcurrentStack<string> candidates = new ConcurrentStack<string>();

                Directory.EnumerateDirectories(rootPath, "*", SearchOption.TopDirectoryOnly)
                    .AsParallel()
                    .ForAll(fld => candidates.PushRange(GetSvnWorkingCopyPaths(fld).ToArray()));

                return candidates.ToList();
            }
        }

        private Dictionary<string, SvnMappingDetails> BuildNewMappings(string rootPath, string sourceBranch, Dictionary<string, SvnMappingDetails> distinctMappings)
        {
            Dictionary<string, SvnMappingDetails> mappings = new Dictionary<string, SvnMappingDetails>();

            if (distinctMappings != null && distinctMappings.Count > 0)
            {
                foreach (KeyValuePair<string, SvnMappingDetails> mapping in distinctMappings)
                {
                    SvnMappingDetails mappingDetails = mapping.Value;

                    string localPath = mappingDetails.LocalPath;
                    string absoluteLocalPath = Path.Combine(rootPath, localPath);

                    SvnMappingDetails newMappingDetails = new SvnMappingDetails();

                    newMappingDetails.ServerPath = mappingDetails.ServerPath;
                    newMappingDetails.LocalPath = absoluteLocalPath;
                    newMappingDetails.Revision = mappingDetails.Revision;
                    newMappingDetails.Depth = mappingDetails.Depth;
                    newMappingDetails.IgnoreExternals = mappingDetails.IgnoreExternals;

                    mappings.Add(absoluteLocalPath, newMappingDetails);
                }
            }
            else
            {
                SvnMappingDetails newMappingDetails = new SvnMappingDetails();

                newMappingDetails.ServerPath = sourceBranch;
                newMappingDetails.LocalPath = rootPath;
                newMappingDetails.Revision = "HEAD";
                newMappingDetails.Depth = 3;  //Infinity
                newMappingDetails.IgnoreExternals = true;

                mappings.Add(rootPath, newMappingDetails);
            }

            return mappings;
        }

        /// <summary>
        /// svn info URL --depth empty --revision <sourceRevision> --xml --username <user> --password <password> --non-interactive [--trust-server-cert]
        /// </summary>
        /// <param name="serverPath"></param>
        /// <param name="sourceRevision"></param>
        /// <returns></returns>
        public async Task<long> GetLatestRevisionAsync(string serverPath, string sourceRevision)
        {
            _context.Debug($@"Get latest revision of: '{_repository.Url.AbsoluteUri}' at or before: '{sourceRevision}'.");
            string xml = await RunPorcelainCommandAsync(
                "info",
                BuildSvnUri(serverPath),
                "--depth", "empty",
                "--revision", sourceRevision,
                "--xml");

            // Deserialize the XML.
            // The command returns a non-zero exit code if the source revision is not found.
            // The assertions performed here should never fail.
            XmlSerializer serializer = new XmlSerializer(typeof(SvnInfo));
            PluginUtil.NotNullOrEmpty(xml, nameof(xml));

            using (StringReader reader = new StringReader(xml))
            {
                SvnInfo info = serializer.Deserialize(reader) as SvnInfo;
                PluginUtil.NotNull(info, nameof(info));
                PluginUtil.NotNull(info.Entries, nameof(info.Entries));
                PluginUtil.Equal(1, info.Entries.Length, nameof(info.Entries.Length));

                long revision = 0;
                long.TryParse(info.Entries[0].Commit?.Revision ?? sourceRevision, out revision);

                return revision;
            }
        }

        /// <summary>
        /// Finds a local path the provided server path is mapped to.
        /// </summary>
        /// <param name="serverPath"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public string ResolveServerPath(string serverPath, string rootPath)
        {
            PluginUtil.Equal(true, serverPath.StartsWith(@"^/"), nameof(serverPath));

            foreach (string workingDirectoryPath in GetSvnWorkingCopyPaths(rootPath))
            {
                try
                {
                    _context.Debug($@"Get SVN info for the working directory path '{workingDirectoryPath}'.");
                    string xml = RunPorcelainCommandAsync(
                        "info",
                        workingDirectoryPath,
                        "--depth", "empty",
                        "--xml").GetAwaiter().GetResult();

                    // Deserialize the XML.
                    // The command returns a non-zero exit code if the local path is not a working copy.
                    // The assertions performed here should never fail.
                    XmlSerializer serializer = new XmlSerializer(typeof(SvnInfo));
                    PluginUtil.NotNullOrEmpty(xml, nameof(xml));

                    using (StringReader reader = new StringReader(xml))
                    {
                        SvnInfo info = serializer.Deserialize(reader) as SvnInfo;
                        PluginUtil.NotNull(info, nameof(info));
                        PluginUtil.NotNull(info.Entries, nameof(info.Entries));
                        PluginUtil.Equal(1, info.Entries.Length, nameof(info.Entries.Length));

                        if (serverPath.Equals(info.Entries[0].RelativeUrl, StringComparison.Ordinal) || serverPath.StartsWith(info.Entries[0].RelativeUrl + '/', StringComparison.Ordinal))
                        {
                            // We've found the mapping the serverPath belongs to.
                            int n = info.Entries[0].RelativeUrl.Length;
                            string relativePath = serverPath.Length <= n + 1 ? string.Empty : serverPath.Substring(n + 1);

                            return Path.Combine(workingDirectoryPath, relativePath);
                        }
                    }
                }
                catch (ProcessExitCodeException)
                {
                    _context.Debug($@"The path '{workingDirectoryPath}' is not an SVN working directory path.");
                }
            }

            _context.Debug($@"Haven't found any suitable mapping for '{serverPath}'");

            // Since the server path starts with the "^/" prefix we return the original path without these two characters.
            return serverPath.Substring(2);
        }

        private async Task<Uri> GetRootUrlAsync(string localPath)
        {
            _context.Debug($@"Get URL for: '{localPath}'.");
            try
            {
                string xml = await RunPorcelainCommandAsync(
                    "info",
                    localPath,
                    "--depth", "empty",
                    "--xml");

                // Deserialize the XML.
                // The command returns a non-zero exit code if the local path is not a working copy.
                // The assertions performed here should never fail.
                XmlSerializer serializer = new XmlSerializer(typeof(SvnInfo));
                PluginUtil.NotNullOrEmpty(xml, nameof(xml));

                using (StringReader reader = new StringReader(xml))
                {
                    SvnInfo info = serializer.Deserialize(reader) as SvnInfo;
                    PluginUtil.NotNull(info, nameof(info));
                    PluginUtil.NotNull(info.Entries, nameof(info.Entries));
                    PluginUtil.Equal(1, info.Entries.Length, nameof(info.Entries.Length));

                    return new Uri(info.Entries[0].Url);
                }
            }
            catch (ProcessExitCodeException)
            {
                _context.Debug($@"The folder '{localPath}.svn' seems not to be a subversion system directory.");
                return null;
            }
        }

        private async Task UpdateToRevisionAsync(Dictionary<string, Uri> oldMappings, Dictionary<string, SvnMappingDetails> newMappings, long maxRevision)
        {
            foreach (KeyValuePair<string, SvnMappingDetails> mapping in newMappings)
            {
                string localPath = mapping.Key;
                SvnMappingDetails mappingDetails = mapping.Value;

                string effectiveServerUri = BuildSvnUri(mappingDetails.ServerPath);
                string effectiveRevision = EffectiveRevision(mappingDetails.Revision, maxRevision);

                mappingDetails.Revision = effectiveRevision;

                if (!Directory.Exists(Path.Combine(localPath, ".svn")))
                {
                    _context.Debug(String.Format(
                        "Checking out with depth: {0}, revision: {1}, ignore externals: {2}",
                        mappingDetails.Depth,
                        effectiveRevision,
                        mappingDetails.IgnoreExternals));

                    mappingDetails.ServerPath = effectiveServerUri;
                    await CheckoutAsync(mappingDetails);
                }
                else if (oldMappings.ContainsKey(localPath) && oldMappings[localPath].Equals(new Uri(effectiveServerUri)))
                {
                    _context.Debug(String.Format(
                        "Updating with depth: {0}, revision: {1}, ignore externals: {2}",
                        mappingDetails.Depth,
                        mappingDetails.Revision,
                        mappingDetails.IgnoreExternals));

                    await UpdateAsync(mappingDetails);
                }
                else
                {
                    _context.Debug(String.Format(
                        "Switching to {0}  with depth: {1}, revision: {2}, ignore externals: {3}",
                        mappingDetails.ServerPath,
                        mappingDetails.Depth,
                        mappingDetails.Revision,
                        mappingDetails.IgnoreExternals));

                    await SwitchAsync(mappingDetails);
                }
            }
        }

        private string EffectiveRevision(string mappingRevision, long maxRevision)
        {
            if (!mappingRevision.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                // A specific revision has been requested in mapping
                return mappingRevision;
            }
            else if (maxRevision == 0)
            {
                // Tip revision
                return "HEAD";
            }
            else
            {
                return maxRevision.ToString();
            }
        }

        private void CleanUpSvnWorkspace(Dictionary<string, Uri> oldMappings, Dictionary<string, SvnMappingDetails> newMappings)
        {
            _context.Debug("Clean up Svn workspace.");
            oldMappings.Where(m => !newMappings.ContainsKey(m.Key))
                .AsParallel()
                .ForAll(m =>
                {
                    _context.Debug($@"Delete unmapped folder: '{m.Key}'");
                    PluginUtil.DeleteDirectory(m.Key, CancellationToken.None);
                });
        }

        /// <summary>
        /// svn update localPath --depth empty --revision <sourceRevision> --xml --username lin --password ideafix --non-interactive [--trust-server-cert]
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        private async Task UpdateAsync(SvnMappingDetails mapping)
        {
            _context.Debug($@"Update '{mapping.LocalPath}'.");
            await RunCommandAsync(
                "update",
                mapping.LocalPath,
                "--revision", mapping.Revision,
                "--depth", ToDepthArgument(mapping.Depth),
                mapping.IgnoreExternals ? "--ignore-externals" : null);
        }

        /// <summary>
        /// svn switch localPath --depth empty --revision <sourceRevision> --xml --username lin --password ideafix --non-interactive [--trust-server-cert]
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        private async Task SwitchAsync(SvnMappingDetails mapping)
        {
            _context.Debug($@"Switch '{mapping.LocalPath}' to '{mapping.ServerPath}'.");
            await RunCommandAsync(
                "switch",
                $"^/{mapping.ServerPath}",
                mapping.LocalPath,
                "--ignore-ancestry",
                "--revision", mapping.Revision,
                "--depth", ToDepthArgument(mapping.Depth),
                mapping.IgnoreExternals ? "--ignore-externals" : null);
        }

        /// <summary>
        /// svn checkout localPath --depth empty --revision <sourceRevision> --xml --username lin --password ideafix --non-interactive [--trust-server-cert]
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        private async Task CheckoutAsync(SvnMappingDetails mapping)
        {
            _context.Debug($@"Checkout '{mapping.ServerPath}' to '{mapping.LocalPath}'.");
            await RunCommandAsync(
                "checkout",
                mapping.ServerPath,
                mapping.LocalPath,
                "--revision", mapping.Revision,
                "--depth", ToDepthArgument(mapping.Depth),
                mapping.IgnoreExternals ? "--ignore-externals" : null);
        }

        private string BuildSvnUri(string serverPath)
        {
            StringBuilder sb = new StringBuilder(_repository.Url.ToString());

            if (!string.IsNullOrEmpty(serverPath))
            {
                if (sb[sb.Length - 1] != '/')
                {
                    sb.Append('/');
                }
                sb.Append(serverPath);
            }

            return sb.Replace('\\', '/').ToString();
        }

        private string FormatArgumentsWithDefaults(params string[] args)
        {
            // Format each arg.
            List<string> formattedArgs = new List<string>();
            foreach (string arg in args ?? new string[0])
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    // Validate the arg.
                    if (arg.IndexOfAny(new char[] { '"', '\r', '\n' }) >= 0)
                    {
                        throw new Exception(PluginUtil.Loc("InvalidCommandArg", arg));
                    }

                    // Add the arg.
                    formattedArgs.Add(QuotedArgument(arg));
                }
            }

            // Add the common parameters.
            if (_acceptUntrusted)
            {
                formattedArgs.Add("--trust-server-cert");
            }

            if (!string.IsNullOrWhiteSpace(_username))
            {
                formattedArgs.Add("--username");
                formattedArgs.Add(QuotedArgument(_username));
            }

            if (!string.IsNullOrWhiteSpace(_password))
            {
                formattedArgs.Add("--password");
                formattedArgs.Add(QuotedArgument(_password));
            }

            formattedArgs.Add("--no-auth-cache"); // Do not cache credentials
            formattedArgs.Add("--non-interactive");

            // Add proxy setting parameters
            var agentProxy = _context.GetProxyConfiguration();
            if (agentProxy != null && !string.IsNullOrEmpty(agentProxy.ProxyAddress) && !agentProxy.IsBypassed(_repository.Url))
            {
                _context.Debug($"Add proxy setting parameters to '{_svn}' for proxy server '{agentProxy.ProxyAddress}'.");

                formattedArgs.Add("--config-option");
                formattedArgs.Add(QuotedArgument($"servers:global:http-proxy-host={new Uri(agentProxy.ProxyAddress).Host}"));

                formattedArgs.Add("--config-option");
                formattedArgs.Add(QuotedArgument($"servers:global:http-proxy-port={new Uri(agentProxy.ProxyAddress).Port}"));

                if (!string.IsNullOrEmpty(agentProxy.ProxyUsername))
                {
                    formattedArgs.Add("--config-option");
                    formattedArgs.Add(QuotedArgument($"servers:global:http-proxy-username={agentProxy.ProxyUsername}"));
                }

                if (!string.IsNullOrEmpty(agentProxy.ProxyPassword))
                {
                    formattedArgs.Add("--config-option");
                    formattedArgs.Add(QuotedArgument($"servers:global:http-proxy-password={agentProxy.ProxyPassword}"));
                }
            }

            return string.Join(" ", formattedArgs);
        }

        private string QuotedArgument(string arg)
        {
            char quote = '\"';
            char altQuote = '\'';

            if (arg.IndexOf(quote) > -1)
            {
                quote = '\'';
                altQuote = '\"';
            }

            return (arg.IndexOfAny(new char[] { ' ', altQuote }) == -1) ? arg : $"{quote}{arg}{quote}";
        }

        private string ToDepthArgument(int depth)
        {
            switch (depth)
            {
                case 0:
                    return "empty";
                case 1:
                    return "files";
                case 2:
                    return "immediates";
                default:
                    return "infinity";
            }
        }

        private async Task RunCommandAsync(params string[] args)
        {
            // Validation.
            PluginUtil.NotNull(args, nameof(args));
            PluginUtil.NotNull(_context, nameof(_context));

            // Invoke tf.
            var processInvoker = new ProcessInvoker(_context);
            var outputLock = new object();
            processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    _context.Output(e.Data);
                }
            };
            processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    _context.Output(e.Data);
                }
            };
            string arguments = FormatArgumentsWithDefaults(args);
            _context.Command($@"{_svn} {arguments}");
            await processInvoker.ExecuteAsync(
                workingDirectory: _context.Variables.GetValueOrDefault("agent.workfolder")?.Value,
                fileName: _svn,
                arguments: arguments,
                environment: null,
                requireExitCodeZero: true,
                cancellationToken: _cancellationToken);

        }

        private async Task<string> RunPorcelainCommandAsync(params string[] args)
        {
            // Validation.
            PluginUtil.NotNull(args, nameof(args));
            PluginUtil.NotNull(_context, nameof(_context));

            // Invoke tf.
            var processInvoker = new ProcessInvoker(_context);
            var output = new List<string>();
            var outputLock = new object();
            processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    _context.Debug(e.Data);
                    output.Add(e.Data);
                }
            };
            processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    _context.Debug(e.Data);
                    output.Add(e.Data);
                }
            };
            string arguments = FormatArgumentsWithDefaults(args);
            _context.Debug($@"{_svn} {arguments}");
            // TODO: Test whether the output encoding needs to be specified on a non-Latin OS.
            try
            {
                await processInvoker.ExecuteAsync(
                    workingDirectory: _context.Variables.GetValueOrDefault("agent.workfolder")?.Value,
                    fileName: _svn,
                    arguments: arguments,
                    environment: null,
                    requireExitCodeZero: true,
                    cancellationToken: _cancellationToken);
            }
            catch (ProcessExitCodeException)
            {
                // The command failed. Dump the output and throw.
                output.ForEach(x => _context.Output(x ?? string.Empty));
                throw;
            }

            // Note, string.join gracefully handles a null element within the IEnumerable<string>.
            return string.Join(Environment.NewLine, output);

        }

        /// <summary>
        /// Removes unused and duplicate mappings.
        /// </summary>
        /// <param name="allMappings"></param>
        /// <returns></returns>
        public Dictionary<string, SvnMappingDetails> NormalizeMappings(List<SvnMappingDetails> allMappings)
        {
            // We use Ordinal comparer because SVN is case sensetive and keys in the dictionary are URLs.
            Dictionary<string, SvnMappingDetails> distinctMappings = new Dictionary<string, SvnMappingDetails>(StringComparer.Ordinal);
            HashSet<string> localPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (SvnMappingDetails map in allMappings)
            {
                string localPath = NormalizeRelativePath(map.LocalPath, Path.DirectorySeparatorChar, '/');
                string serverPath = NormalizeRelativePath(map.ServerPath, '/', '\\');

                if (string.IsNullOrEmpty(serverPath))
                {
                    _context.Debug(PluginUtil.Loc("SvnEmptyServerPath", localPath));
                    _context.Debug(PluginUtil.Loc("SvnMappingIgnored"));

                    distinctMappings.Clear();
                    distinctMappings.Add(string.Empty, map);
                    break;
                }

                if (localPaths.Contains(localPath))
                {
                    _context.Debug(PluginUtil.Loc("SvnMappingDuplicateLocal", localPath));
                    continue;
                }
                else
                {
                    localPaths.Add(localPath);
                }

                if (distinctMappings.ContainsKey(serverPath))
                {
                    _context.Debug(PluginUtil.Loc("SvnMappingDuplicateServer", serverPath));
                    continue;
                }

                // Put normalized values of the local and server paths back into the mapping.
                map.LocalPath = localPath;
                map.ServerPath = serverPath;

                distinctMappings.Add(serverPath, map);
            }

            return distinctMappings;
        }

        /// <summary>
        /// Normalizes path separator for server and local paths.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathSeparator"></param>
        /// <param name="altPathSeparator"></param>
        /// <returns></returns>
        public string NormalizeRelativePath(string path, char pathSeparator, char altPathSeparator)
        {
            string relativePath = (path ?? string.Empty).Replace(altPathSeparator, pathSeparator);
            relativePath = relativePath.Trim(pathSeparator, ' ');

            if (relativePath.Contains(":") || relativePath.Contains(".."))
            {
                throw new Exception(PluginUtil.Loc("SvnIncorrectRelativePath", relativePath));
            }

            return relativePath;
        }


        // The cancellation token used to stop svn command execution
        private CancellationToken _cancellationToken;

        // The Subversion repository providing URL and untrusted certs acceptace information
        private Pipelines.RepositoryResource _repository;

        // The build commands' execution context
        private AgentTaskPluginExecutionContext _context;

        // The svn command line utility location
        private string _svn;

        // The svn user name from SVN repository connection endpoint
        private string _username;

        // The svn user password from SVN repository connection endpoint
        private string _password;

        // The acceptUntrustedCerts property from SVN repository connection endpoint
        private bool _acceptUntrusted;
    }

    ////////////////////////////////////////////////////////////////////////////////
    // svn info data objects
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "info", Namespace = "")]
    public sealed class SvnInfo
    {
        [XmlElement(ElementName = "entry", Namespace = "")]
        public SvnInfoEntry[] Entries { get; set; }
    }

    public sealed class SvnInfoEntry
    {
        [XmlAttribute(AttributeName = "kind", Namespace = "")]
        public string Kind { get; set; }

        [XmlAttribute(AttributeName = "path", Namespace = "")]
        public string Path { get; set; }

        [XmlAttribute(AttributeName = "revision", Namespace = "")]
        public string Revision { get; set; }

        [XmlElement(ElementName = "url", Namespace = "")]
        public string Url { get; set; }

        [XmlElement(ElementName = "relative-url", Namespace = "")]
        public string RelativeUrl { get; set; }

        [XmlElement(ElementName = "repository", Namespace = "")]
        public SvnInfoRepository[] Repository { get; set; }

        [XmlElement(ElementName = "wc-info", Namespace = "", IsNullable = true)]
        public SvnInfoWorkingCopy[] WorkingCopyInfo { get; set; }

        [XmlElement(ElementName = "commit", Namespace = "")]
        public SvnInfoCommit Commit { get; set; }
    }

    public sealed class SvnInfoRepository
    {
        [XmlElement(ElementName = "wcroot-abspath", Namespace = "")]
        public string AbsPath { get; set; }

        [XmlElement(ElementName = "schedule", Namespace = "")]
        public string Schedule { get; set; }

        [XmlElement(ElementName = "depth", Namespace = "")]
        public string Depth { get; set; }
    }

    public sealed class SvnInfoWorkingCopy
    {
        [XmlElement(ElementName = "root", Namespace = "")]
        public string Root { get; set; }

        [XmlElement(ElementName = "uuid", Namespace = "")]
        public Guid Uuid { get; set; }
    }

    public sealed class SvnInfoCommit
    {
        [XmlAttribute(AttributeName = "revision", Namespace = "")]
        public string Revision { get; set; }

        [XmlElement(ElementName = "author", Namespace = "")]
        public string Author { get; set; }

        [XmlElement(ElementName = "date", Namespace = "")]
        public string Date { get; set; }
    }
}
