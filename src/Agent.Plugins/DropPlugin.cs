using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Agent.Sdk;

namespace Agent.Plugins.Drop
{
    public class ArtifactUploadCommand : IAgentCommandPlugin
    {
        public string Area => "artifact";

        public string Event => "upload";

        public string DisplayName => PluginUtil.Loc("UploadArtifact");

        public async Task ProcessCommandAsync(AgentCommandPluginExecutionContext context, CancellationToken token)
        {
            PluginUtil.NotNull(context, nameof(context));

            Guid projectId = new Guid(context.Variables.GetValueOrDefault(BuildVariables.TeamProjectId)?.Value ?? Guid.Empty.ToString());
            PluginUtil.NotEmpty(projectId, nameof(projectId));

            string buildIdStr = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty;
            if (!int.TryParse(buildIdStr, out int buildId))
            {
                throw new ArgumentOutOfRangeException(buildIdStr);
            }

            string containerIdStr = context.Variables.GetValueOrDefault(BuildVariables.ContainerId)?.Value ?? string.Empty;
            if (!long.TryParse(containerIdStr, out long containerId))
            {
                throw new ArgumentOutOfRangeException(buildIdStr);
            }

            string artifactName;
            if (!context.Properties.TryGetValue(ArtifactUploadEventProperties.ArtifactName, out artifactName) ||
                string.IsNullOrEmpty(artifactName))
            {
                throw new Exception(PluginUtil.Loc("ArtifactNameRequired"));
            }

            string containerFolder;
            if (!context.Properties.TryGetValue(ArtifactUploadEventProperties.ContainerFolder, out containerFolder) ||
                string.IsNullOrEmpty(containerFolder))
            {
                containerFolder = artifactName;
            }

            var propertyDictionary = ExtractArtifactProperties(context.Properties);

            string localPath = context.Data;
            if (context.ContainerPathMappings.Count > 0)
            {
                // Translate file path back from container path
                localPath = context.TranslateContainerPathToHostPath(localPath);
            }

            if (string.IsNullOrEmpty(localPath))
            {
                throw new Exception(PluginUtil.Loc("ArtifactLocationRequired"));
            }

            string hostType = context.Variables.GetValueOrDefault("system.hosttype")?.Value;
            if (!IsUncSharePath(context, localPath) && !string.Equals(hostType, "Build", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(PluginUtil.Loc("UploadArtifactCommandNotSupported", hostType ?? string.Empty));
            }

            string fullPath = Path.GetFullPath(localPath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                // if localPath is not a file or folder on disk
                throw new FileNotFoundException(PluginUtil.Loc("PathNotExist", localPath));
            }
            else if (Directory.Exists(fullPath) && Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories).FirstOrDefault() == null)
            {
                // if localPath is a folder but the folder contains nothing
                context.Output(PluginUtil.Loc("DirectoryIsEmptyForArtifact", fullPath, artifactName));
                return;
            }

            // Upload to file container
            context.Output(PluginUtil.Loc("UploadArtifact"));
            FileContainerServer fileContainerHelper = new FileContainerServer(context.VssConnection, projectId, containerId, containerFolder);
            await fileContainerHelper.CopyToContainerAsync(context, fullPath, token);
            string fileContainerFullPath = PluginUtil.Format($"#/{containerId}/{containerFolder}");
            context.Output(PluginUtil.Loc("UploadToFileContainer", fullPath, fileContainerFullPath));

            // Associate build artifact
            BuildServer buildHelper = new BuildServer(context.VssConnection);
            var artifact = await buildHelper.AssociateArtifact(projectId, buildId, artifactName, ArtifactResourceTypes.Container, fileContainerFullPath, propertyDictionary, token);
            context.Output(PluginUtil.Loc("AssociateArtifactWithBuild", artifact.Id, buildId));
        }


        private static class ArtifactUploadEventProperties
        {
            public static readonly string ContainerFolder = "containerfolder";
            public static readonly string ArtifactName = "artifactname";
            public static readonly string ArtifactType = "artifacttype";
            public static readonly string Browsable = "Browsable";
        }

        private Dictionary<string, string> ExtractArtifactProperties(Dictionary<string, string> eventProperties)
        {
            return eventProperties.Where(pair => !(string.Compare(pair.Key, ArtifactUploadEventProperties.ContainerFolder, StringComparison.OrdinalIgnoreCase) == 0 ||
                                                  string.Compare(pair.Key, ArtifactUploadEventProperties.ArtifactName, StringComparison.OrdinalIgnoreCase) == 0 ||
                                                  string.Compare(pair.Key, ArtifactUploadEventProperties.ArtifactType, StringComparison.OrdinalIgnoreCase) == 0)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private Boolean IsUncSharePath(AgentCommandPluginExecutionContext context, string path)
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
    }
}
