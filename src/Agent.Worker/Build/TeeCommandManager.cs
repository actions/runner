using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TeeCommandManager : TfsVCCommandManager, ITfsVCCommandManager
    {
        public override TfsVCFeatures Features => TfsVCFeatures.Eula;

        protected override string Switch => "-";

        protected override string TF
        {
            get
            {
                return Path.Combine(
                    IOUtil.GetExternalsPath(),
                    Constants.Path.TeeDirectory,
                    "tf");
            }
        }

        public async Task EulaAsync()
        {
            await RunCommandAsync(FormatFlags.All, "eula", "-accept");
        }

        public async Task GetAsync(string localPath)
        {
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync(FormatFlags.OmitCollectionUrl, "get", $"-version:{SourceVersion}", "-recursive", "-overwrite", localPath);
        }

        public string ResolvePath(string serverPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            string localPath = RunPorcelainCommandAsync("resolvePath", $"-workspace:{WorkspaceName}", serverPath).GetAwaiter().GetResult();
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

        public Task ScorchAsync()
        {
            throw new NotSupportedException();
        }

        public async Task ShelveAsync(string shelveset, string commentFile)
        {
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            ArgUtil.NotNullOrEmpty(commentFile, nameof(commentFile));
            await RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "shelve", $"-workspace:{WorkspaceName}", "-saved", "-replace", "-recursive", $"-comment:@{commentFile}", shelveset);
        }

        public async Task<ITfsVCShelveset> ShelvesetsAsync(string shelveset)
        {
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            string xml = await RunPorcelainCommandAsync("shelvesets", "-format:xml", $"-workspace:{WorkspaceName}", shelveset);

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

        public async Task<ITfsVCStatus> StatusAsync(string localPath)
        {
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            string xml = await RunPorcelainCommandAsync(FormatFlags.OmitCollectionUrl, "status", "-recursive", "-format:xml", localPath);
            var serializer = new XmlSerializer(typeof(TeeStatus));
            using (var reader = new StringReader(xml ?? string.Empty))
            {
                return serializer.Deserialize(reader) as TeeStatus;
            }
        }

        public bool TestEulaAccepted()
        {
            Trace.Entering();

            // Resolve the path to the XML file containing the EULA-accepted flag.
            string homeDirectory = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(homeDirectory) && Directory.Exists(homeDirectory))
            {
#if OS_OSX
                string xmlFile = Path.Combine(
                    homeDirectory,
                    "Library",
                    "Application Support",
                    "Microsoft",
                    "Team Foundation",
                    "4.0",
                    "Configuration",
                    "TEE-Mementos",
                    "com.microsoft.tfs.client.productid.xml");
#else
                string xmlFile = Path.Combine(
                    homeDirectory,
                    ".microsoft",
                    "Team Foundation",
                    "4.0",
                    "Configuration",
                    "TEE-Mementos",
                    "com.microsoft.tfs.client.productid.xml");
#endif
                if (File.Exists(xmlFile))
                {
                    // Load and deserialize the XML.
                    string xml = File.ReadAllText(xmlFile, Encoding.UTF8);
                    XmlSerializer serializer = new XmlSerializer(typeof(ProductIdData));
                    using (var reader = new StringReader(xml ?? string.Empty))
                    {
                        var data = serializer.Deserialize(reader) as ProductIdData;
                        return string.Equals(data?.Eula?.Value ?? string.Empty, "true", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return false;
        }

        public async Task<bool> TryWorkspaceDeleteAsync(ITfsVCWorkspace workspace)
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

        public async Task UndoAsync(string localPath)
        {
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync(FormatFlags.OmitCollectionUrl, "undo", "-recursive", localPath);
        }

        public async Task UnshelveAsync(string shelveset)
        {
            ArgUtil.NotNullOrEmpty(shelveset, nameof(shelveset));
            await RunCommandAsync("unshelve", "-format:detailed", $"-workspace:{WorkspaceName}", shelveset);
        }

        public async Task WorkfoldCloakAsync(string serverPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            await RunCommandAsync("workfold", "-cloak", $"-workspace:{WorkspaceName}", serverPath);
        }

        public async Task WorkfoldMapAsync(string serverPath, string localPath)
        {
            ArgUtil.NotNullOrEmpty(serverPath, nameof(serverPath));
            ArgUtil.NotNullOrEmpty(localPath, nameof(localPath));
            await RunCommandAsync("workfold", "-map", $"-workspace:{WorkspaceName}", serverPath, localPath);
        }

        public Task WorkfoldUnmapAsync(string serverPath)
        {
            throw new NotSupportedException();
        }

        public async Task WorkspaceNewAsync()
        {
            await RunCommandAsync("workspace", "-new", "-location:server", "-permission:Public", WorkspaceName);
        }

        public async Task<ITfsVCWorkspace[]> WorkspacesAsync()
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
                return (serializer.Deserialize(reader) as TeeWorkspaces)
                    ?.Workspaces
                    ?.Cast<ITfsVCWorkspace>()
                    .ToArray();
            }
        }

        public async Task WorkspacesRemoveAsync(ITfsVCWorkspace workspace)
        {
            ArgUtil.NotNull(workspace, nameof(workspace));
            await RunCommandAsync("workspace", $"-remove:{workspace.Name};{workspace.Owner}");
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Product ID data objects (required for testing whether the EULA has been accepted).
        ////////////////////////////////////////////////////////////////////////////////
        [XmlRoot(ElementName = "ProductIdData", Namespace = "")]
        public sealed class ProductIdData
        {
            [XmlElement(ElementName = "eula-14.0", Namespace = "")]
            public Eula Eula { get; set; }
        }

        public sealed class Eula
        {
            [XmlAttribute(AttributeName = "value", Namespace = "")]
            public string Value { get; set; }
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

    public sealed class TeeShelveset : ITfsVCShelveset
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
    public sealed class TeeStatus : ITfsVCStatus
    {
        // Elements.
        [XmlArray(ElementName = "candidate-pending-changes", Namespace = "")]
        [XmlArrayItem(ElementName = "pending-change", Namespace = "")]
        public TeePendingChange[] CandidatePendingChanges { get; set; }

        [XmlArray(ElementName = "pending-changes", Namespace = "")]
        [XmlArrayItem(ElementName = "pending-change", Namespace = "")]
        public TeePendingChange[] PendingChanges { get; set; }

        // Interface-only properties.
        [XmlIgnore]
        public IEnumerable<ITfsVCPendingChange> AllAdds
        {
            get
            {
                return PendingChanges?.Where(x => string.Equals(x.ChangeType, "add", StringComparison.OrdinalIgnoreCase));
            }
        }

        [XmlIgnore]
        public bool HasPendingChanges => PendingChanges?.Any() ?? false;
    }

    public sealed class TeePendingChange : ITfsVCPendingChange
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

    public sealed class TeeWorkspace : ITfsVCWorkspace
    {
        // Attributes.
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

        // Elements.
        [XmlElement(ElementName = "working-folder", Namespace = "")]
        public TeeMapping[] TeeMappings { get; set; }

        // Interface-only properties.
        [XmlIgnore]
        public ITfsVCMapping[] Mappings => TeeMappings?.Cast<ITfsVCMapping>().ToArray();
    }

    public sealed class TeeMapping : ITfsVCMapping
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