using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

using Newtonsoft.Json;

using ServerBuildArtifact = Microsoft.TeamFoundation.Build.WebApi.BuildArtifact;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    // TODO: Write tests for this
    public class BuildArtifact : AgentService, IArtifactExtension
    {
        public Type ExtensionType => typeof(IArtifactExtension);
        public AgentArtifactType ArtifactType => AgentArtifactType.Build;

        private const string AllArtifacts = "*";

        public async Task DownloadAsync(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));

            int buildId = Convert.ToInt32(artifactDefinition.Version, CultureInfo.InvariantCulture);
            if (buildId <= 0)
            {
                throw new ArgumentException("artifactDefinition.Version");
            }

            var buildArtifactDetails = artifactDefinition.Details as BuildArtifactDetails;
            if (buildArtifactDetails == null)
            {
                throw new ArgumentException("artifactDefinition.Details");
            }

            // Get the list of available artifacts from build. 
            executionContext.Output(StringUtil.Loc("RMPreparingToGetBuildArtifactList"));

            var vssConnection = new VssConnection(buildArtifactDetails.TfsUrl, buildArtifactDetails.Credentials);
            var buildClient = vssConnection.GetClient<BuildHttpClient>();
            var xamlBuildClient = vssConnection.GetClient<XamlBuildHttpClient>();
            List<ServerBuildArtifact> buildArtifacts = null;
            DefinitionType buildDefinitionType = DefinitionType.Build;

            try
            {
                buildArtifacts = await buildClient.GetArtifactsAsync(buildArtifactDetails.Project, buildId);
            }
            catch (BuildNotFoundException)
            {
                buildArtifacts = await xamlBuildClient.GetArtifactsAsync(buildArtifactDetails.Project, buildId);
                buildDefinitionType = DefinitionType.Xaml;
            }

            // No artifacts found in the build => Fail it. 
            if (buildArtifacts == null || !buildArtifacts.Any())
            {
                throw new ArtifactDownloadException(StringUtil.Loc("RMNoBuildArtifactsFound", buildId));
            }

            // DownloadFromStream each of the artifact sequentially. 
            // TODO: Should we download them parallely?
            foreach (ServerBuildArtifact buildArtifact in buildArtifacts)
            {
                if (Match(buildArtifact, artifactDefinition))
                {
                    executionContext.Output(StringUtil.Loc("RMPreparingToDownload", buildArtifact.Name));
                    await this.DownloadArtifactAsync(executionContext, buildArtifact, artifactDefinition, localFolderPath, buildClient, xamlBuildClient, buildDefinitionType, buildId);
                }
                else
                {
                    executionContext.Warning(StringUtil.Loc("RMArtifactMatchNotFound", buildArtifact.Name));
                }
            }
        }

        public IArtifactDetails GetArtifactDetails(IExecutionContext context, AgentArtifactDefinition agentArtifactDefinition)
        {
            Trace.Entering();

            ServiceEndpoint vssEndpoint = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(vssEndpoint, nameof(vssEndpoint));
            ArgUtil.NotNull(vssEndpoint.Url, nameof(vssEndpoint.Url));

            var artifactDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(agentArtifactDefinition.Details);
            VssCredentials vssCredentials = ApiUtil.GetVssCredential(vssEndpoint);
            var tfsUrl = context.Variables.Get(WellKnownDistributedTaskVariables.TFCollectionUrl);

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            string relativePath;
            string accessToken;
            vssEndpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken);

            if (artifactDetails.TryGetValue("RelativePath", out relativePath))
            {
                return new BuildArtifactDetails
                {
                    Credentials = vssCredentials,
                    RelativePath = artifactDetails["RelativePath"],
                    Project = projectId.ToString(),
                    TfsUrl = new Uri(tfsUrl),
                };
            }
            else
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete"));
            }
        }

        private bool Match(ServerBuildArtifact buildArtifact, ArtifactDefinition artifactDefinition)
        {
            //TODO: If editing artifactDefinitionName is not allowed then we can remove this
            if (string.Equals(artifactDefinition.Name, AllArtifacts, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(artifactDefinition.Name, buildArtifact.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private async Task DownloadArtifactAsync(
            IExecutionContext executionContext,
            ServerBuildArtifact buildArtifact,
            ArtifactDefinition artifactDefinition,
            string localFolderPath,
            BuildHttpClient buildClient,
            XamlBuildHttpClient xamlBuildClient,
            DefinitionType definitionType,
            int buildId)
        {
            var downloadFolderPath = Path.Combine(localFolderPath, buildArtifact.Name);
            var buildArtifactDetails = artifactDefinition.Details as BuildArtifactDetails;
            if ((buildArtifact.Resource.Type == null && buildArtifact.Id == 0) // bug on build API Bug 378900
                || string.Equals(buildArtifact.Resource.Type, WellKnownArtifactResourceTypes.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                executionContext.Output("Artifact Type: FileShare");
                string fileShare;
                if (buildArtifact.Id == 0)
                {
                    fileShare = new Uri(buildArtifact.Resource.DownloadUrl).LocalPath;
                }
                else
                {
                    fileShare = new Uri(Path.Combine(buildArtifact.Resource.DownloadUrl, buildArtifact.Name)).LocalPath;
                    if (!Directory.Exists(fileShare))
                    {
                        // download path does not exist, log and fall back
                        var parenthPath = new Uri(buildArtifact.Resource.DownloadUrl).LocalPath;
                        executionContext.Output(StringUtil.Loc("RMArtifactNameDirectoryNotFound", fileShare, parenthPath));
                        fileShare = parenthPath;
                    }
                }

                if (!Directory.Exists(fileShare))
                {
                    // download path does not exist, raise exception
                    throw new ArtifactDownloadException(StringUtil.Loc("RMArtifactDirectoryNotFoundError", fileShare));
                }

                executionContext.Output(StringUtil.Loc("RMDownloadingArtifactFromFileShare", fileShare));

                var fileShareArtifact = new FileShareArtifact();
                await fileShareArtifact.DownloadArtifactAsync(executionContext, HostContext, artifactDefinition, fileShare, downloadFolderPath);
            }
            else if (string.Equals(buildArtifact.Resource.Type, WellKnownArtifactResourceTypes.Container, StringComparison.OrdinalIgnoreCase))
            {
                executionContext.Output("Artifact Type: ServerDrop");

                // TODO:Get VssBinFetchclient and get away from zipstream downloader
                Stream contentStream;
                
                if (definitionType == DefinitionType.Xaml)
                {
                    contentStream = await xamlBuildClient.GetArtifactContentZipAsync(buildArtifactDetails.Project, buildId, buildArtifact.Name);
                }
                else
                {
                    contentStream = await buildClient.GetArtifactContentZipAsync(buildArtifactDetails.Project, buildId, buildArtifact.Name);
                }
                

                var zipStreamDownloader = HostContext.GetService<IZipStreamDownloader>();
                string artifactRootFolder = StringUtil.Format("/{0}", buildArtifact.Name);
                await zipStreamDownloader.DownloadFromStream(contentStream, artifactRootFolder, buildArtifactDetails.RelativePath, downloadFolderPath);
            }
            else
            {
                executionContext.Warning(StringUtil.Loc("RMArtifactTypeNotSupported", buildArtifact.Resource.Type));
            }
        }
    }
}