using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TFCommandManager : TfsVCCommandManager, ITfsVCCommandManager
    {
        public override TfsVCFeatures Features
        {
            get
            {
                return TfsVCFeatures.DefaultWorkfoldMap |
                    TfsVCFeatures.EscapedUrl |
                    TfsVCFeatures.GetFromUnmappedRoot |
                    TfsVCFeatures.LoginType |
                    TfsVCFeatures.Scorch;
            }
        }

        // When output is redirected, TF.exe writes output using the current system code page
        // (i.e. CP_ACP or code page 0). E.g. code page 1252 on an en-US box.
        protected override Encoding OutputEncoding => StringUtil.GetSystemEncoding();

        protected override string Switch => "/";

        public string FilePath => Path.Combine(ExecutionContext.Variables.Agent_ServerOMDirectory, "tf.exe");

        private string AppConfigFile => Path.Combine(ExecutionContext.Variables.Agent_ServerOMDirectory, "tf.exe.config");

        private string AppConfigRestoreFile => Path.Combine(ExecutionContext.Variables.Agent_ServerOMDirectory, "tf.exe.config.restore");

        // TODO: Remove AddAsync after last-saved-checkin-metadata problem is fixed properly.
        public async Task AddAsync(string localPath)
        {
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "add", localPath);
        }

        public void CleanupProxySetting()
        {
            ArgUtil.File(AppConfigRestoreFile, "tf.exe.config.restore");
            ExecutionContext.Debug("Restore default tf.exe.config.");
            IOUtil.DeleteFile(AppConfigFile);
            File.Copy(AppConfigRestoreFile, AppConfigFile);
        }

        public Task EulaAsync()
        {
            throw new NotSupportedException();
        }

        public async Task GetAsync(string localPath)
        {
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "get", $"/version:{SourceVersion}", "/recursive", "/overwrite", localPath);
        }

        public string ResolvePath(string serverPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            string localPath = RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "resolvePath", serverPath).GetAwaiter().GetResult();
            return localPath?.Trim() ?? string.Empty;
        }

        // TODO: Fix scorch. Scorch blows up if a root mapping does not exist.
        //
        // No good workaround appears to exist. Attempting to resolve by workspace fails with
        // the same error. Switching to "*" instead of passing "SourcesDirectory" allows the
        // command to exit zero, but causes every source file to be deleted.
        //
        // The current approach taken is: allow the exception to bubble. The TfsVCSourceProvider
        // will catch the exception, log it as a warning, throw away the workspace, and re-clone.
        public async Task ScorchAsync() => await RunCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "scorch", SourcesDirectory, "/recursive", "/diff", "/unmapped");

        public void SetupProxy(string proxyUrl, string proxyUsername, string proxyPassword)
        {
            ArgUtil.File(AppConfigFile, "tf.exe.config");
            if (!File.Exists(AppConfigRestoreFile))
            {
                Trace.Info("Take snapshot of current appconfig for restore modified appconfig.");
                File.Copy(AppConfigFile, AppConfigRestoreFile);
            }
            else
            {
                // cleanup any appconfig changes from previous build.
                CleanupProxySetting();
            }

            if (!string.IsNullOrEmpty(proxyUrl))
            {
                XmlDocument appConfig = new XmlDocument();
                using (var appConfigStream = new FileStream(AppConfigFile, FileMode.Open, FileAccess.Read))
                {
                    appConfig.Load(appConfigStream);
                }

                var configuration = appConfig.SelectSingleNode("configuration");
                ArgUtil.NotNull(configuration, "configuration");

                var exist_defaultProxy = appConfig.SelectSingleNode("configuration/system.net/defaultProxy");
                if (exist_defaultProxy == null)
                {
                    var system_net = appConfig.SelectSingleNode("configuration/system.net");
                    if (system_net == null)
                    {
                        Trace.Verbose("Create system.net section in appconfg.");
                        system_net = appConfig.CreateElement("system.net");
                    }

                    Trace.Verbose("Create defaultProxy section in appconfg.");
                    var defaultProxy = appConfig.CreateElement("defaultProxy");
                    defaultProxy.SetAttribute("useDefaultCredentials", "True");

                    Trace.Verbose("Create proxy section in appconfg.");
                    var proxy = appConfig.CreateElement("proxy");
                    proxy.SetAttribute("proxyaddress", proxyUrl);

                    defaultProxy.AppendChild(proxy);
                    system_net.AppendChild(defaultProxy);
                    configuration.AppendChild(system_net);

                    using (var appConfigStream = new FileStream(AppConfigFile, FileMode.Open, FileAccess.ReadWrite))
                    {
                        appConfig.Save(appConfigStream);
                    }
                }
                else
                {
                    //proxy setting exist.
                    ExecutionContext.Debug("Proxy setting already exist in app.config file.");
                }

                // when tf.exe talk to any devfabric site, it will always bypass proxy. 
                // for testing, we need set this variable to let tf.exe hit the proxy server on devfabric.
                if (Endpoint.Url.Host.Contains(".me.tfsallin.net") || Endpoint.Url.Host.Contains(".vsts.me"))
                {
                    ExecutionContext.Debug("Set TFS_BYPASS_PROXY_ON_LOCAL on devfabric.");
                    AdditionalEnvironmentVariables["TFS_BYPASS_PROXY_ON_LOCAL"] = "0";
                }
            }
        }

        public async Task ShelveAsync(string shelveset, string commentFile, bool move)
        {
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            ArgUtil.NotNullOrEmpty(commentFile, nameof(commentFile));

            // TODO: Remove parameter "move" after last-saved-checkin-metadata problem is fixed properly.
            if (move)
            {
                await RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "shelve", "/move", "/replace", "/recursive", $"/comment:@{commentFile}", shelveset, SourcesDirectory);
                return;
            }

            await RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "shelve", "/saved", "/replace", "/recursive", $"/comment:@{commentFile}", shelveset, SourcesDirectory);
        }

        public async Task<ITfsVCShelveset> ShelvesetsAsync(string shelveset)
        {
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            string xml = await RunPorcelainCommandAsync("vc", "shelvesets", "/format:xml", shelveset);

            // Deserialize the XML.
            // The command returns a non-zero exit code if the shelveset is not found.
            // The assertions performed here should never fail.
            var serializer = new XmlSerializer(typeof(TFShelvesets));
            ArgUtil.NotNullOrEmpty(xml, nameof(xml));
            using (var reader = new StringReader(xml))
            {
                var tfShelvesets = serializer.Deserialize(reader) as TFShelvesets;
                ArgUtil.NotNull(tfShelvesets, nameof(tfShelvesets));
                ArgUtil.NotNull(tfShelvesets.Shelvesets, nameof(tfShelvesets.Shelvesets));
                ArgUtil.Equal(1, tfShelvesets.Shelvesets.Length, nameof(tfShelvesets.Shelvesets.Length));
                return tfShelvesets.Shelvesets[0];
            }
        }

        public async Task<ITfsVCStatus> StatusAsync(string localPath)
        {
            // It is expected that the caller only invokes this method against the sources root
            // directory. The "status" subcommand cannot correctly resolve the workspace from the
            // an unmapped root folder. For example, if a workspace contains only two mappings,
            // $/foo -> $(build.sourcesDirectory)\foo and $/bar -> $(build.sourcesDirectory)\bar,
            // then "tf status $(build.sourcesDirectory) /r" will not be able to resolve the workspace.
            // Therefore, the "localPath" parameter is not actually passed to the "status" subcommand -
            // the collection URL and workspace name are used instead.
            ArgUtil.Equal(SourcesDirectory, localPath, nameof(localPath));
            string xml = await RunPorcelainCommandAsync("vc", "status", $"/workspace:{WorkspaceName}", "/recursive", "/nodetect", "/format:xml");
            var serializer = new XmlSerializer(typeof(TFStatus));
            using (var reader = new StringReader(xml ?? string.Empty))
            {
                return serializer.Deserialize(reader) as TFStatus;
            }
        }

        public bool TestEulaAccepted()
        {
            throw new NotSupportedException();
        }

        public async Task<bool> TryWorkspaceDeleteAsync(ITfsVCWorkspace workspace)
        {
            ArgUtil.NotNull(workspace, nameof(workspace));
            try
            {
                await RunCommandAsync("vc", "workspace", "/delete", $"{workspace.Name};{workspace.Owner}");
                return true;
            }
            catch (Exception ex)
            {
                ExecutionContext.Warning(ex.Message);
                return false;
            }
        }

        public async Task UndoAsync(string localPath)
        {
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "undo", "/recursive", localPath);
        }

        public async Task UnshelveAsync(string shelveset)
        {
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            await RunCommandAsync(FormatFlags.OmitCollectionUrl, "vc", "unshelve", shelveset);
        }

        public async Task WorkfoldCloakAsync(string serverPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            await RunCommandAsync("vc", "workfold", "/cloak", $"/workspace:{WorkspaceName}", serverPath);
        }

        public async Task WorkfoldMapAsync(string serverPath, string localPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync("vc", "workfold", "/map", $"/workspace:{WorkspaceName}", serverPath, localPath);
        }

        public async Task WorkfoldUnmapAsync(string serverPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            await RunCommandAsync("vc", "workfold", "/unmap", $"/workspace:{WorkspaceName}", serverPath);
        }

        public async Task WorkspaceDeleteAsync(ITfsVCWorkspace workspace)
        {
            ArgUtil.NotNull(workspace, nameof(workspace));
            await RunCommandAsync("vc", "workspace", "/delete", $"{workspace.Name};{workspace.Owner}");
        }

        public async Task WorkspaceNewAsync()
        {
            await RunCommandAsync("vc", "workspace", "/new", "/location:local", "/permission:Public", WorkspaceName);
        }

        public async Task<ITfsVCWorkspace[]> WorkspacesAsync(bool matchWorkspaceNameOnAnyComputer = false)
        {
            // Build the args.
            var args = new List<string>();
            args.Add("vc");
            args.Add("workspaces");
            if (matchWorkspaceNameOnAnyComputer)
            {
                args.Add(WorkspaceName);
                args.Add($"/computer:*");
            }

            args.Add("/format:xml");

            // Run the command.
            string xml = await RunPorcelainCommandAsync(args.ToArray()) ?? string.Empty;

            // Deserialize the XML.
            var serializer = new XmlSerializer(typeof(TFWorkspaces));
            using (var reader = new StringReader(xml))
            {
                return (serializer.Deserialize(reader) as TFWorkspaces)
                    ?.Workspaces
                    ?.Cast<ITfsVCWorkspace>()
                    .ToArray();
            }
        }

        public async Task WorkspacesRemoveAsync(ITfsVCWorkspace workspace)
        {
            ArgUtil.NotNull(workspace, nameof(workspace));
            await RunCommandAsync("vc", "workspace", $"/remove:{workspace.Name};{workspace.Owner}");
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf shelvesets data objects
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "Shelvesets", Namespace = "")]
    public sealed class TFShelvesets
    {
        [XmlElement(ElementName = "Shelveset", Namespace = "")]
        public TFShelveset[] Shelvesets { get; set; }
    }

    public sealed class TFShelveset : ITfsVCShelveset
    {
        // Attributes.
        [XmlAttribute(AttributeName = "date", Namespace = "")]
        public string Date { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "owner", Namespace = "")]
        public string Owner { get; set; }

        // Elements.
        [XmlElement(ElementName = "Comment", Namespace = "")]
        public string Comment { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf status data objects.
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "Status", Namespace = "")]
    public sealed class TFStatus : ITfsVCStatus
    {
        // Elements.
        [XmlElement(ElementName = "PendingSet", Namespace = "")]
        public TFPendingSet[] PendingSets { get; set; }

        // Interface-only properties.
        [XmlIgnore]
        public IEnumerable<ITfsVCPendingChange> AllAdds
        {
            get
            {
                return PendingSets
                    ?.SelectMany(x => x.PendingChanges ?? new TFPendingChange[0])
                    .Where(x => (x.ChangeType ?? string.Empty).Split(' ').Any(y => string.Equals(y, "Add", StringComparison.OrdinalIgnoreCase)));
            }
        }

        [XmlIgnore]
        public bool HasPendingChanges => PendingSets?.Any(x => x.PendingChanges?.Any() ?? false) ?? false;
    }

    public sealed class TFPendingSet
    {
        // Attributes.
        [XmlAttribute(AttributeName = "computer", Namespace = "")]
        public string Computer { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "owner", Namespace = "")]
        public string Owner { get; set; }

        [XmlAttribute(AttributeName = "ownerdisp", Namespace = "")]
        public string OwnerDisplayName { get; set; }

        [XmlAttribute(AttributeName = "ownership", Namespace = "")]
        public string Ownership { get; set; }

        // Elements.
        [XmlArray(ElementName = "PendingChanges", Namespace = "")]
        [XmlArrayItem(ElementName = "PendingChange", Namespace = "")]
        public TFPendingChange[] PendingChanges { get; set; }
    }

    public sealed class TFPendingChange : ITfsVCPendingChange
    {
        // Attributes.
        [XmlAttribute(AttributeName = "chg", Namespace = "")]
        public string ChangeType { get; set; }

        [XmlAttribute(AttributeName = "date", Namespace = "")]
        public string Date { get; set; }

        [XmlAttribute(AttributeName = "enc", Namespace = "")]
        public string Encoding { get; set; }

        [XmlAttribute(AttributeName = "hash", Namespace = "")]
        public string Hash { get; set; }

        [XmlAttribute(AttributeName = "item", Namespace = "")]
        public string Item { get; set; }

        [XmlAttribute(AttributeName = "itemid", Namespace = "")]
        public string ItemId { get; set; }

        [XmlAttribute(AttributeName = "local", Namespace = "")]
        public string LocalItem { get; set; }

        [XmlAttribute(AttributeName = "pcid", Namespace = "")]
        public string PCId { get; set; }

        [XmlAttribute(AttributeName = "psn", Namespace = "")]
        public string Psn { get; set; }

        [XmlAttribute(AttributeName = "pso", Namespace = "")]
        public string Pso { get; set; }

        [XmlAttribute(AttributeName = "psod", Namespace = "")]
        public string Psod { get; set; }

        [XmlAttribute(AttributeName = "srcitem", Namespace = "")]
        public string SourceItem { get; set; }

        [XmlAttribute(AttributeName = "svrfm", Namespace = "")]
        public string Svrfm { get; set; }

        [XmlAttribute(AttributeName = "type", Namespace = "")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "uhash", Namespace = "")]
        public string UHash { get; set; }

        [XmlAttribute(AttributeName = "ver", Namespace = "")]
        public string Version { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf workspaces data objects.
    ////////////////////////////////////////////////////////////////////////////////
    [XmlRoot(ElementName = "Workspaces", Namespace = "")]
    public sealed class TFWorkspaces
    {
        [XmlElement(ElementName = "Workspace", Namespace = "")]
        public TFWorkspace[] Workspaces { get; set; }
    }

    public sealed class TFWorkspace : ITfsVCWorkspace
    {
        // Attributes.
        [XmlAttribute(AttributeName = "computer", Namespace = "")]
        public string Computer { get; set; }

        [XmlAttribute(AttributeName = "islocal", Namespace = "")]
        public string IsLocal { get; set; }

        [XmlAttribute(AttributeName = "name", Namespace = "")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "owner", Namespace = "")]
        public string Owner { get; set; }

        [XmlAttribute(AttributeName = "ownerdisp", Namespace = "")]
        public string OwnerDisplayName { get; set; }

        [XmlAttribute(AttributeName = "ownerid", Namespace = "")]
        public string OwnerId { get; set; }

        [XmlAttribute(AttributeName = "ownertype", Namespace = "")]
        public string OwnerType { get; set; }

        [XmlAttribute(AttributeName = "owneruniq", Namespace = "")]
        public string OwnerUnique { get; set; }

        // Elements.
        [XmlArray(ElementName = "Folders", Namespace = "")]
        [XmlArrayItem(ElementName = "WorkingFolder", Namespace = "")]
        public TFMapping[] TFMappings { get; set; }

        // Interface-only properties.
        [XmlIgnore]
        public ITfsVCMapping[] Mappings => TFMappings?.Cast<ITfsVCMapping>().ToArray();
    }

    public sealed class TFMapping : ITfsVCMapping
    {
        [XmlIgnore]
        public bool Cloak => string.Equals(Type, "Cloak", StringComparison.OrdinalIgnoreCase);

        [XmlAttribute(AttributeName = "depth", Namespace = "")]
        public string Depth { get; set; }

        [XmlAttribute(AttributeName = "local", Namespace = "")]
        public string LocalPath { get; set; }

        [XmlIgnore]
        public bool Recursive => !string.Equals(Depth, "1", StringComparison.OrdinalIgnoreCase);

        [XmlAttribute(AttributeName = "item", Namespace = "")]
        public string ServerPath { get; set; }

        [XmlAttribute(AttributeName = "type", Namespace = "")]
        public string Type { get; set; }
    }
}