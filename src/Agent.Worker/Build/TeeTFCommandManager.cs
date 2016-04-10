using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    [ServiceLocator(Default = typeof(TeeTFCommandManager))]
    public interface ITeeTFCommandManager : IAgentService
    {
        CancellationToken CancellationToken { set; }
        ServiceEndpoint Endpoint { set; }
        IExecutionContext ExecutionContext { set; }

        Task GetAsync(string version, string directory);
        string ResolvePath(string workspace, string serverPath);
        Task ShelveAsync(string directory, string shelveset, string commentFile);
        Task<TeeShelveset> ShelvesetsAsync(string workspace, string shelveset);
        Task<TeeStatus> StatusAsync(string workspace);
        Task<bool> TryWorkspaceDeleteAsync(TeeWorkspace workspace);
        Task UndoAsync(string directory);
        Task UnshelveAsync(string workspace, string shelveset);
        Task WorkfoldCloakAsync(string workspace, string serverPath);
        Task WorkfoldMapAsync(string workspace, string serverPath, string localPath);
        Task WorkspaceNewAsync(string name);
        Task<TeeWorkspace[]> WorkspacesAsync();
        Task WorkspacesRemoveAsync(TeeWorkspace workspace);
    }

    // TODO: Accept EULA during configure.
    public sealed class TeeTFCommandManager : AgentService, ITeeTFCommandManager
    {
        private readonly string _tf = Path.Combine(IOUtil.GetExternalsPath(), "tee", "tf");

        public CancellationToken CancellationToken { private get; set; }
        public ServiceEndpoint Endpoint { private get; set; }
        public IExecutionContext ExecutionContext { private get; set; }

        public async Task GetAsync(string version, string directory)
        {
            ArgUtil.NotNullOrEmpty(version, nameof(version));
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));
            await RunCommandAsync("get", $"-version:{version}", "-recursive", "-overwrite", directory);
        }

        public string ResolvePath(string workspace, string serverPath)
        {
            ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            string localPath = RunPorcelainCommandAsync("resolvePath", $"-workspace:{workspace}", serverPath).GetAwaiter().GetResult();
            localPath = localPath?.Trim();

            // Paths outside of the root mapping return empty.
            // Paths within a cloaked directory return "null".
            if (string.IsNullOrEmpty(localPath) ||
                string.Equals(localPath, "null", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return localPath;
        }

        public async Task ShelveAsync(string directory, string shelveset, string commentFile)
        {
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            ArgUtil.NotNullOrEmpty(commentFile, nameof(commentFile));
            await RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "shelve", "-replace", "-recursive", $"-comment:@{commentFile}", shelveset, directory);
        }

        public async Task<TeeShelveset> ShelvesetsAsync(string workspace, string shelveset)
        {
            ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            string xml = await RunPorcelainCommandAsync("shelvesets", "-format:xml", $"-workspace:{workspace}", shelveset);

            // Deserialize the XML.
            // The command returns a non-zero exit code if the shelveset is not found.
            // The assertions performed here should never fail.
            var serializer = new XmlSerializer(typeof(TeeShelvesets));
            ArgUtil.NotNullOrEmpty(xml, nameof(xml));
            using (var reader = new StringReader(xml))
            {
                var teeShelvesets = serializer.Deserialize(reader) as TeeShelvesets;
                ArgUtil.NotNull(teeShelvesets, nameof(teeShelvesets));
                ArgUtil.NotNull(teeShelvesets.Shelvesets, nameof(teeShelvesets.Shelvesets));
                ArgUtil.Equal(1, teeShelvesets.Shelvesets.Length, nameof(teeShelvesets.Shelvesets.Length));
                return teeShelvesets.Shelvesets[0];
            }
        }

        public async Task<TeeStatus> StatusAsync(string workspace)
        {
            ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
            string xml = await RunPorcelainCommandAsync("status", $"-workspace:{workspace}", "-recursive", "-format:xml");
            var serializer = new XmlSerializer(typeof(TeeStatus));
            using (var reader = new StringReader(xml ?? string.Empty))
            {
                return serializer.Deserialize(reader) as TeeStatus;
            }
        }

        public async Task<bool> TryWorkspaceDeleteAsync(TeeWorkspace workspace)
        {
            ArgUtil.NotNull(workspace, nameof(workspace));
            try
            {
                await RunCommandAsync("workspace", "-delete", $"{workspace.Name};{workspace.Owner}");
                return true;
            }
            catch (Exception ex)
            {
                ExecutionContext.Warning(ex.Message);
                return false;
            }
        }

        public async Task UndoAsync(string directory)
        {
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));
            await RunCommandAsync("undo", "-recursive", directory);
        }

        public async Task UnshelveAsync(string workspace, string shelveset)
        {
            ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            await RunCommandAsync("unshelve", "-format:detailed", $"-workspace:{workspace}", shelveset);
        }

        public async Task WorkfoldCloakAsync(string workspace, string serverPath)
        {
            ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            await RunCommandAsync("workfold", "-cloak", $"-workspace:{workspace}", serverPath);
        }

        public async Task WorkfoldMapAsync(string workspace, string serverPath, string localPath)
        {
            ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync("workfold", "-map", $"-workspace:{workspace}", serverPath, localPath);
        }

        public async Task WorkspaceNewAsync(string name)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            await RunCommandAsync("workspace", "-new", "-location:local", "-permission:Public", name);
        }

        public async Task<TeeWorkspace[]> WorkspacesAsync()
        {
            string xml = await RunPorcelainCommandAsync("workspaces", "-format:xml") ?? string.Empty;

            // The command returns a non-XML message preceeding the XML if there are no workspaces.
            if (!xml.StartsWith("<?xml"))
            {
                return null;
            }

            // Deserialize the XML.
            var serializer = new XmlSerializer(typeof(TeeWorkspaces));
            using (var reader = new StringReader(xml))
            {
                return (serializer.Deserialize(reader) as TeeWorkspaces)?.Workspaces;
            }
        }

        public async Task WorkspacesRemoveAsync(TeeWorkspace workspace)
        {
            ArgUtil.NotNull(workspace, nameof(workspace));
            await RunCommandAsync("workspace", $"-remove:{workspace.Name};{workspace.Owner}");
        }

        private string FormatArguments(FormatFlags formatFlags, params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(Endpoint, nameof(Endpoint));
            ArgUtil.NotNull(Endpoint.Authorization, nameof(Endpoint.Authorization));
            ArgUtil.NotNull(Endpoint.Authorization.Parameters, nameof(Endpoint.Authorization.Parameters));
            ArgUtil.Equal(EndpointAuthorizationSchemes.OAuth, Endpoint.Authorization.Scheme, nameof(Endpoint.Authorization.Scheme));
            string accessToken = Endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken) ? accessToken : null;
            ArgUtil.NotNullOrEmpty(accessToken, EndpointAuthorizationParameters.AccessToken);
            ArgUtil.NotNull(Endpoint.Url, nameof(Endpoint.Url));

            // Format each arg.
            var formattedArgs = new List<string>();
            foreach (string arg in args ?? new string[0])
            {
                // Validate the arg.
                if (!string.IsNullOrEmpty(arg) && arg.IndexOfAny(new char[] { '"', '\r', '\n' }) >= 0)
                {
                    // TODO: LOC
                    throw new Exception($@"Argument '{arg}' contains one or more of the invalid characters: "", \r, \n");
                }

                // Add the arg.
                formattedArgs.Add(arg != null && arg.Contains(" ") ? $@"""{arg}""" : $"{arg}");
            }

            // Add the common parameters.
            if (!formatFlags.HasFlag(FormatFlags.OmitCollectionUrl))
            {
                formattedArgs.Add($"-collection:{Endpoint.Url.AbsoluteUri}");
            }

            formattedArgs.Add($"-login:_,{accessToken}");
            formattedArgs.Add("-noprompt");
            return string.Join(" ", formattedArgs);
        }

        private async Task RunCommandAsync(params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            // Invoke tf.
            using(var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                var outputLock = new object();
                processInvoker.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Output(e.Data);
                    }
                };
                processInvoker.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Output(e.Data);
                    }
                };
                string arguments = FormatArguments(FormatFlags.None, args);
                ExecutionContext.Command($@"{_tf} {arguments}");
                int exitCode = await processInvoker.ExecuteAsync(
                    workingDirectory: IOUtil.GetWorkPath(HostContext),
                    fileName: _tf,
                    arguments: arguments,
                    environment: null,
                    cancellationToken: CancellationToken);
                if (exitCode != 0)
                {
                    // TODO: LOC
                    throw new Exception($"Exit code {exitCode} returned from command: {_tf} {arguments}");
                }
            }
        }

        private Task<string> RunPorcelainCommandAsync(params string[] args)
        {
            return RunPorcelainCommandAsync(FormatFlags.None, args);
        }

        private async Task<string> RunPorcelainCommandAsync(FormatFlags formatFlags, params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            // Invoke tf.
            using(var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                var output = new StringBuilder();
                var outputLock = new object();
                processInvoker.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Debug(e.Data);
                        output.AppendLine(e.Data);
                    }
                };
                processInvoker.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Debug(e.Data);
                        output.AppendLine(e.Data);
                    }
                };
                string arguments = FormatArguments(formatFlags, args);
                ExecutionContext.Debug($@"{_tf} {arguments}");
                // TODO: Test whether the output encoding needs to be specified for the process.
                int exitCode = await processInvoker.ExecuteAsync(
                    workingDirectory: IOUtil.GetWorkPath(HostContext),
                    fileName: _tf,
                    arguments: arguments,
                    environment: null,
                    cancellationToken: CancellationToken);
                if (exitCode != 0)
                {
                    // The command failed. Dump the output and throw.
                    ExecutionContext.Output(output.ToString());
                    // TODO: LOC
                    throw new Exception($"Exit code {exitCode} returned from command: {_tf} {arguments}");
                }

                return output.ToString();
            }
        }

        [Flags]
        private enum FormatFlags
        {
            None = 0,
            OmitCollectionUrl = 1
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf shelvesets data objects
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "shelvesets", Namespace = "")]
    public sealed class TeeShelvesets
    {
        [XmlElement(ElementName = "shelveset", Namespace = "")]
        public TeeShelveset[] Shelvesets { get; set; }
    }

    public sealed class TeeShelveset
    {
        [XmlAttribute(AttributeName = "date", Namespace = "")]
        public string Date { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "owner", Namespace = "")]
        public string Owner { get; set; }

        [XmlElement(ElementName = "comment", Namespace = "")]
        public string Comment { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf status data objects.
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "status", Namespace = "")]
    public sealed class TeeStatus
    {
        [XmlArray(ElementName = "candidate-pending-changes", Namespace = "")]
        [XmlArrayItem(ElementName = "pending-change", Namespace = "")]
        public TeePendingChange[] CandidatePendingChanges { get; set; }

        [XmlArray(ElementName = "pending-changes", Namespace = "")]
        [XmlArrayItem(ElementName = "pending-change", Namespace = "")]
        public TeePendingChange[] PendingChanges { get; set; }
    }

    public sealed class TeePendingChange
    {
        [XmlAttribute(AttributeName = "change-type", Namespace = "")]
        public string ChangeType { get; set; }

        [XmlAttribute(AttributeName = "computer", Namespace = "")]
        public string Computer { get; set; }

        [XmlAttribute(AttributeName = "date", Namespace = "")]
        public string Date { get; set; }

        [XmlAttribute(AttributeName = "file-type", Namespace = "")]
        public string FileType { get; set; }

        [XmlAttribute(AttributeName = "local-item", Namespace = "")]
        public string LocalItem { get; set; }

        [XmlAttribute(AttributeName = "lock", Namespace = "")]
        public string Lock { get; set; }

        [XmlAttribute(AttributeName = "owner", Namespace = "")]
        public string Owner { get; set; }

        [XmlAttribute(AttributeName = "server-item", Namespace = "")]
        public string ServerItem { get; set; }

        [XmlAttribute(AttributeName = "version", Namespace = "")]
        public string Version { get; set; }

        [XmlAttribute(AttributeName = "workspace", Namespace = "")]
        public string Workspace { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf workspaces data objects.
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "workspaces", Namespace = "")]
    public sealed class TeeWorkspaces
    {
        [XmlElement(ElementName = "workspace", Namespace = "")]
        public TeeWorkspace[] Workspaces { get; set; }
    }

    public sealed class TeeWorkspace
    {
        [XmlAttribute(AttributeName = "server", Namespace = "")]
        public string CollectionUrl { get; set; }

        [XmlAttribute(AttributeName = "comment", Namespace = "")]
        public string Comment { get; set; }

        [XmlAttribute(AttributeName = "computer", Namespace = "")]
        public string Computer { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "owner", Namespace = "")]
        public string Owner { get; set; }

        [XmlElement(ElementName = "working-folder", Namespace = "")]
        public TeeMapping[] Mappings { get; set; }
    }

    public sealed class TeeMapping
    {
        [XmlIgnore]
        public bool Cloak => string.Equals(MappingType, "cloak", StringComparison.OrdinalIgnoreCase);

        [XmlAttribute(AttributeName = "depth", Namespace = "")]
        public string Depth { get; set; }

        [XmlAttribute(AttributeName = "local-item", Namespace = "")]
        public string LocalPath { get; set; }

        [XmlAttribute(AttributeName = "type", Namespace = "")]
        public string MappingType { get; set; }

        [XmlIgnore]
        public bool Recursive => string.Equals(Depth, "full", StringComparison.OrdinalIgnoreCase);

        [XmlAttribute(AttributeName = "server-item")]
        public string ServerPath { get; set; }
    }
}