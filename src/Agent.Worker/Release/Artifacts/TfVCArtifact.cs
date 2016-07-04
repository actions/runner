using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.FormInput;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Newtonsoft.Json;
using DefinitionWorkspaceMappings = Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCSourceProvider.DefinitionWorkspaceMappings;
using DefinitionWorkspaceMapping = Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCSourceProvider.DefinitionWorkspaceMapping;
using DefinitionMappingType = Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCSourceProvider.DefinitionMappingType;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class TfVCArtifact : AgentService, IArtifactExtension
    {
        public Type ExtensionType => typeof(IArtifactExtension);
        public AgentArtifactType ArtifactType => AgentArtifactType.Tfvc;
        public async Task DownloadAsync(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string downloadFolderPath)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNullOrEmpty(downloadFolderPath, nameof(downloadFolderPath));

            var tfVCDetails = artifactDefinition.Details as TfVCArtifactDetails;
            ArgUtil.NotNull(tfVCDetails, nameof(tfVCDetails));

            ServiceEndpoint tfVCEndpoint = executionContext.Endpoints.FirstOrDefault((e => string.Equals(e.Name, tfVCDetails.RepositoryId, StringComparison.OrdinalIgnoreCase)));
            if (tfVCEndpoint == null)
            {
                throw new InvalidOperationException(StringUtil.Loc("RMTfVCEndpointNotFound"));
            }

            PrepareTfVCEndpoint(tfVCEndpoint, tfVCDetails);
            var extensionManager = HostContext.GetService<IExtensionManager>();
            ISourceProvider sourceProvider = (extensionManager.GetExtensions<ISourceProvider>()).FirstOrDefault(x => x.RepositoryType == WellKnownRepositoryTypes.TfsVersionControl);

            if (sourceProvider == null)
            {
                throw new InvalidOperationException(StringUtil.Loc("SourceArtifactNotFound", WellKnownRepositoryTypes.TfsVersionControl));
            }

            var rootDirectory = Directory.GetParent(downloadFolderPath).Name;
            executionContext.Variables.Set(Constants.Variables.Agent.BuildDirectory, rootDirectory);
            executionContext.Variables.Set(Constants.Variables.Build.SourcesDirectory, downloadFolderPath);
            executionContext.Variables.Set(Constants.Variables.Build.SourceVersion, artifactDefinition.Version);

            await sourceProvider.GetSourceAsync(executionContext, tfVCEndpoint, executionContext.CancellationToken);
        }

        public IArtifactDetails GetArtifactDetails(IExecutionContext context, AgentArtifactDefinition agentArtifactDefinition)
        {
            var artifactDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(agentArtifactDefinition.Details);
            var projectId = string.Empty;
            var repositoryId = string.Empty;
            var mappings = string.Empty;

            if (!artifactDetails.TryGetValue(ArtifactDefinitionConstants.MappingsId, out mappings) || mappings == null)
            {
                string baseRepoPath = string.Join("/", "$", projectId);
                var defaultMapping = new List<Dictionary<string, InputValue>>()
                                         {
                                             new Dictionary<string, InputValue>()
                                                 {
                                                     {
                                                         ArtifactDefinitionConstants.ServerPathId,
                                                         new InputValue { Value = baseRepoPath }
                                                     },
                                                     {
                                                         ArtifactDefinitionConstants.MappingTypeId,
                                                         new InputValue { Value = Constants.Release.Map }
                                                     },
                                                     {
                                                         ArtifactDefinitionConstants.LocalPathId,
                                                         new InputValue { Value = string.Empty }
                                                     }
                                                 }
                                         };

                mappings = JsonConvert.SerializeObject(defaultMapping);
            }

            if (artifactDetails.TryGetValue(ArtifactDefinitionConstants.ProjectId, out projectId)
                && artifactDetails.TryGetValue(ArtifactDefinitionConstants.RepositoryId, out repositoryId))
            {
                return new TfVCArtifactDetails
                {
                    RelativePath = "\\",
                    ProjectId = projectId,
                    RepositoryId = repositoryId,
                    Mappings = mappings
                };
            }
            else
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete", agentArtifactDefinition.Name));
            }
        }

        private void PrepareTfVCEndpoint(ServiceEndpoint endpoint, TfVCArtifactDetails tfVcArtifactDetails)
        {
            var allMappings = JsonConvert.DeserializeObject<IList<Dictionary<string, InputValue>>>(tfVcArtifactDetails.Mappings);
            var distinctMapping = new Dictionary<string, DefinitionWorkspaceMapping>();

            foreach (var map in allMappings)
            {
                InputValue mappingServerPath;
                InputValue mappingType;

                if (map.TryGetValue(ArtifactDefinitionConstants.ServerPathId, out mappingServerPath)
                    && map.TryGetValue(ArtifactDefinitionConstants.MappingTypeId, out mappingType)
                    && mappingServerPath != null
                    && mappingType != null)
                {
                    if (!distinctMapping.ContainsKey(mappingServerPath.Value))
                    {
                        InputValue mappingLocalPath;
                        bool isLocalPathPresent = map.TryGetValue(ArtifactDefinitionConstants.LocalPathId, out mappingLocalPath);

                        DefinitionMappingType type;
                        Enum.TryParse(mappingType.Value, out type);
                        distinctMapping.Add(
                            mappingServerPath.Value,
                            new DefinitionWorkspaceMapping
                            {
                                ServerPath = mappingServerPath.Value,
                                MappingType = type,
                                LocalPath = isLocalPathPresent ? mappingLocalPath.Value : string.Empty
                            });
                    }
                }
            }

            var definitionWorkspaceMappings = new DefinitionWorkspaceMappings
            {
                Mappings = distinctMapping.Values.ToArray()
            };

            endpoint.Data.Add(WellKnownEndpointData.TfvcWorkspaceMapping, JsonConvert.SerializeObject(definitionWorkspaceMappings));
            endpoint.Data.Add(WellKnownEndpointData.Clean, "true");
        }
    }
}