using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class ArtifactCommandExtension : AgentService, IWorkerCommandExtension
    {
        public Type ExtensionType => typeof(IWorkerCommandExtension);

        public string CommandArea => "artifact";

        public HostTypes SupportedHostTypes => HostTypes.Build | HostTypes.Release;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (string.Equals(command.Event, WellKnownArtifactCommand.Associate, StringComparison.OrdinalIgnoreCase))
            {
                ProcessArtifactAssociateCommand(context, command.Properties, command.Data);
            }
            else if (string.Equals(command.Event, WellKnownArtifactCommand.Upload, StringComparison.OrdinalIgnoreCase))
            {
                ProcessArtifactUploadCommand(context, command.Properties, command.Data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("ArtifactCommandNotFound", command.Event));
            }
        }

        private void ProcessArtifactAssociateCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(context.Endpoints, nameof(context.Endpoints));

            ServiceEndpoint systemConnection = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(systemConnection, nameof(systemConnection));
            ArgUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            Uri projectUrl = systemConnection.Url;
            VssCredentials projectCredential = ApiUtil.GetVssCredential(systemConnection);

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            int? buildId = context.Variables.Build_BuildId;
            ArgUtil.NotNull(buildId, nameof(buildId));

            string artifactName;
            if (!eventProperties.TryGetValue(ArtifactAssociateEventProperties.ArtifactName, out artifactName) ||
                string.IsNullOrEmpty(artifactName))
            {
                throw new Exception(StringUtil.Loc("ArtifactNameRequired"));
            }

            string artifactType;
            if (!eventProperties.TryGetValue(ArtifactAssociateEventProperties.ArtifactType, out artifactType))
            {
                artifactType = InferArtifactResourceType(context, data);
            }

            if (string.IsNullOrEmpty(artifactType))
            {
                throw new Exception(StringUtil.Loc("ArtifactTypeRequired"));
            }
            else if ((artifactType.Equals(WellKnownArtifactResourceTypes.Container, StringComparison.OrdinalIgnoreCase) ||
                      artifactType.Equals(WellKnownArtifactResourceTypes.FilePath, StringComparison.OrdinalIgnoreCase) ||
                      artifactType.Equals(WellKnownArtifactResourceTypes.VersionControl, StringComparison.OrdinalIgnoreCase)) &&
                     string.IsNullOrEmpty(data))
            {
                throw new Exception(StringUtil.Loc("ArtifactLocationRequired"));
            }

            if (!artifactType.Equals(WellKnownArtifactResourceTypes.FilePath, StringComparison.OrdinalIgnoreCase) &&
                context.Variables.System_HostType != HostTypes.Build)
            {
                throw new Exception(StringUtil.Loc("AssociateArtifactCommandNotSupported", context.Variables.System_HostType));
            }

            var propertyDictionary = ExtractArtifactProperties(eventProperties);

            string artifactData = "";
            if (IsContainerPath(data) ||
                IsValidServerPath(data))
            {
                //if data is a file container path or a tfvc server path
                artifactData = data;
            }
            else if (IsUncSharePath(context, data))
            {
                //if data is a UNC share path
                artifactData = new Uri(data).LocalPath;
            }
            else
            {
                artifactData = data ?? string.Empty;
            }

            // queue async command task to associate artifact.
            context.Debug($"Associate artifact: {artifactName} with build: {buildId.Value} at backend.");
            var commandContext = HostContext.CreateService<IAsyncCommandContext>();
            commandContext.InitializeCommandContext(context, StringUtil.Loc("AssociateArtifact"));
            commandContext.Task = AssociateArtifactAsync(commandContext,
                                                         WorkerUtilities.GetVssConnection(context),
                                                         projectId,
                                                         buildId.Value,
                                                         artifactName,
                                                         artifactType,
                                                         artifactData,
                                                         propertyDictionary,
                                                         context.CancellationToken);
            context.AsyncCommands.Add(commandContext);
        }

        private void ProcessArtifactUploadCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(context.Endpoints, nameof(context.Endpoints));

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            int? buildId = context.Variables.Build_BuildId;
            ArgUtil.NotNull(buildId, nameof(buildId));

            long? containerId = context.Variables.Build_ContainerId;
            ArgUtil.NotNull(containerId, nameof(containerId));

            string artifactName;
            if (!eventProperties.TryGetValue(ArtifactAssociateEventProperties.ArtifactName, out artifactName) ||
                string.IsNullOrEmpty(artifactName))
            {
                throw new Exception(StringUtil.Loc("ArtifactNameRequired"));
            }

            string containerFolder;
            if (!eventProperties.TryGetValue(ArtifactUploadEventProperties.ContainerFolder, out containerFolder) ||
                string.IsNullOrEmpty(containerFolder))
            {
                containerFolder = artifactName;
            }

            var propertyDictionary = ExtractArtifactProperties(eventProperties);

            string localPath = data;
            if (string.IsNullOrEmpty(localPath))
            {
                throw new Exception(StringUtil.Loc("ArtifactLocationRequired"));
            }

            if (!IsUncSharePath(context, localPath) && (context.Variables.System_HostType != HostTypes.Build))
            {
                throw new Exception(StringUtil.Loc("UploadArtifactCommandNotSupported", context.Variables.System_HostType));
            }

            string fullPath = Path.GetFullPath(localPath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                // if localPath is not a file or folder on disk
                throw new FileNotFoundException(StringUtil.Loc("PathNotExist", localPath));
            }
            else if (Directory.Exists(fullPath) && Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories).FirstOrDefault() == null)
            {
                // if localPath is a folder but the folder contains nothing
                context.Warning(StringUtil.Loc("DirectoryIsEmptyForArtifact", fullPath, artifactName));
                return;
            }

            // queue async command task to associate artifact.
            context.Debug($"Upload artifact: {fullPath} to server for build: {buildId.Value} at backend.");
            var commandContext = HostContext.CreateService<IAsyncCommandContext>();
            commandContext.InitializeCommandContext(context, StringUtil.Loc("UploadArtifact"));
            commandContext.Task = UploadArtifactAsync(commandContext,
                                                      WorkerUtilities.GetVssConnection(context),
                                                      projectId,
                                                      containerId.Value,
                                                      containerFolder,
                                                      buildId.Value,
                                                      artifactName,
                                                      propertyDictionary,
                                                      fullPath,
                                                      context.CancellationToken);
            context.AsyncCommands.Add(commandContext);
        }

        private async Task AssociateArtifactAsync(
            IAsyncCommandContext context,
            VssConnection connection,
            Guid projectId,
            int buildId,
            string name,
            string type,
            string data,
            Dictionary<string, string> propertiesDictionary,
            CancellationToken cancellationToken)
        {
            BuildServer buildHelper = new BuildServer(connection, projectId);
            var artifact = await buildHelper.AssociateArtifact(buildId, name, type, data, propertiesDictionary, cancellationToken);
            context.Output(StringUtil.Loc("AssociateArtifactWithBuild", artifact.Id, buildId));
        }

        private async Task UploadArtifactAsync(
            IAsyncCommandContext context,
            VssConnection connection,
            Guid projectId,
            long containerId,
            string containerPath,
            int buildId,
            string name,
            Dictionary<string, string> propertiesDictionary,
            string source,
            CancellationToken cancellationToken)
        {
            FileContainerServer fileContainerHelper = new FileContainerServer(connection, projectId, containerId, containerPath);
            await fileContainerHelper.CopyToContainerAsync(context, source, cancellationToken);
            string fileContainerFullPath = StringUtil.Format($"#/{containerId}/{containerPath}");
            context.Output(StringUtil.Loc("UploadToFileContainer", source, fileContainerFullPath));

            BuildServer buildHelper = new BuildServer(connection, projectId);
            var artifact = await buildHelper.AssociateArtifact(buildId, name, WellKnownArtifactResourceTypes.Container, fileContainerFullPath, propertiesDictionary, cancellationToken);
            context.Output(StringUtil.Loc("AssociateArtifactWithBuild", artifact.Id, buildId));
        }

        private Boolean IsContainerPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                    path.StartsWith("#", StringComparison.OrdinalIgnoreCase);
        }

        private Boolean IsValidServerPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                    path.Length >= 2 &&
                    path[0] == '$' &&
                    (path[1] == '/' || path[1] == '\\');
        }

        private Boolean IsUncSharePath(IExecutionContext context, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            Uri uri;
            // Add try catch to avoid unexpected throw from Uri.Property.
            try
            {
                if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out uri))
                {
                    if (uri.IsAbsoluteUri && uri.IsUnc)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                context.Debug($"Can't determine path: {path} is UNC or not.");
                context.Debug(ex.ToString());
                return false;
            }

            return false;
        }

        private string InferArtifactResourceType(IExecutionContext context, string artifactLocation)
        {
            string type = "";
            if (!string.IsNullOrEmpty(artifactLocation))
            {
                // Prioritize UNC first as leading double-backslash can also match Tfvc VC paths (multiple slashes in a row are ignored)
                if (IsUncSharePath(context, artifactLocation))
                {
                    type = WellKnownArtifactResourceTypes.FilePath;
                }
                else if (IsValidServerPath(artifactLocation))
                {
                    // TFVC artifact
                    type = WellKnownArtifactResourceTypes.VersionControl;
                }
                else if (IsContainerPath(artifactLocation))
                {
                    // file container artifact
                    type = WellKnownArtifactResourceTypes.Container;
                }
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new Exception(StringUtil.Loc("UnableResolveArtifactType", artifactLocation ?? string.Empty));
            }

            return type;
        }

        private Dictionary<string, string> ExtractArtifactProperties(Dictionary<string, string> eventProperties)
        {
            return eventProperties.Where(pair => !(string.Compare(pair.Key, ArtifactUploadEventProperties.ContainerFolder, StringComparison.OrdinalIgnoreCase) == 0 ||
                                                  string.Compare(pair.Key, ArtifactUploadEventProperties.ArtifactName, StringComparison.OrdinalIgnoreCase) == 0 ||
                                                  string.Compare(pair.Key, ArtifactAssociateEventProperties.ArtifactType, StringComparison.OrdinalIgnoreCase) == 0)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    internal static class WellKnownArtifactCommand
    {
        public static readonly string Associate = "associate";
        public static readonly string Upload = "upload";
    }

    internal static class ArtifactAssociateEventProperties
    {
        public static readonly string ArtifactName = "artifactname";
        public static readonly string ArtifactType = "artifacttype";
        public static readonly string Browsable = "Browsable";
    }

    internal static class ArtifactUploadEventProperties
    {
        public static readonly string ContainerFolder = "containerfolder";
        public static readonly string ArtifactName = "artifactname";
        public static readonly string Browsable = "Browsable";
    }
}