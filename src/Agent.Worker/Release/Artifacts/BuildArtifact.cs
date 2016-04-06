using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.FileContainer;

using ServerBuildArtifact = Microsoft.TeamFoundation.Build.WebApi.BuildArtifact;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class BuildArtifact : IArtifact
    {
        public const string AllArtifacts = "*";

        public async Task Download(ArtifactDefinition artifactDefinition, IHostContext hostContext, IExecutionContext executionContext, string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(hostContext, nameof(hostContext));
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
            catch (AggregateException ex)
            {
                if (ex.InnerException != null && ex.InnerException is BuildNotFoundException)
                {
                    buildArtifacts = xamlBuildClient.GetArtifactsAsync(buildArtifactDetails.Project, buildId).Result;
                    buildDefinitionType = DefinitionType.Xaml;
                }
                else
                {
                    throw;
                }
            }

            // No artifacts found in the build => Fail it. 
            if (buildArtifacts == null || !buildArtifacts.Any())
            {
                throw new ArtifactDownloadException(StringUtil.Loc("RMNoBuildArtifactsFound"));
            }

            // DownloadFromStream each of the artifact sequentially. 
            // TODO: Should we download them parallely?
            foreach (ServerBuildArtifact buildArtifact in buildArtifacts)
            {
                if (this.Match(buildArtifact, artifactDefinition))
                {
                    executionContext.Output(StringUtil.Loc("RMPreparingToDownload", buildArtifact.Name));
                    await this.DownloadArtifact(hostContext, executionContext, buildArtifact, artifactDefinition, localFolderPath, buildClient, xamlBuildClient, buildDefinitionType, buildId);
                }
                else
                {
                    executionContext.Warning(StringUtil.Loc("RMArtifactMatchNotFound", buildArtifact.Name));
                }
            }
        }

        private bool Match(ServerBuildArtifact buildArtifact, ArtifactDefinition artifactDefinition)
        {
            // If this is older , then dont force the name checks. 
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

        private async Task DownloadArtifact(
            IHostContext hostContext,
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
                        executionContext.Output(StringUtil.Loc("RMArtifactNameDirectoryNotFound", fileShare, buildArtifact.Resource.DownloadUrl));
                        fileShare = new Uri(buildArtifact.Resource.DownloadUrl).LocalPath;
                    }
                }

                if (!Directory.Exists(fileShare))
                {
                    // download path does not exist, raise exception
                    throw new ArtifactDownloadException(StringUtil.Loc("RMArtifactDirectoryNotFoundError", fileShare));
                }

                var fileShareArtifact = new FileShareArtifact();
                await fileShareArtifact.DownloadArtifact(artifactDefinition, hostContext, executionContext, fileShare, downloadFolderPath);
            }
            else if (string.Equals(buildArtifact.Resource.Type, WellKnownArtifactResourceTypes.Container, StringComparison.OrdinalIgnoreCase))
            {
                executionContext.Output("Artifact Type: ServerDrop");

                // TODO:Get VssBinFetchclient and get away from zipstream downloader
                Stream contentStream;
                try
                {
                    if (definitionType == DefinitionType.Xaml)
                    {
                        contentStream = xamlBuildClient.GetArtifactContentZipAsync(buildArtifactDetails.Project, buildId, buildArtifact.Name).Result;
                    }
                    else
                    {
                        contentStream = buildClient.GetArtifactContentZipAsync(buildArtifactDetails.Project, buildId, buildArtifact.Name).Result;
                    }
                }
                catch (AggregateException ex)
                {
                    var containerItemNotFoundException = ex.InnerException as ContainerItemNotFoundException;

                    if (containerItemNotFoundException != null)
                    {
                        throw new ArtifactDownloadException(StringUtil.Loc("RMNoBuildArtifactsFound"));
                    }

                    throw;
                }

                var zipStreamDownloader = hostContext.GetService<IZipStreamDownloader>();
                string artifactRootFolder = StringUtil.Format("/{0}", buildArtifact.Name);
                await zipStreamDownloader.DownloadFromStream(contentStream, artifactRootFolder, buildArtifactDetails.RelativePath, downloadFolderPath);
            }
            else
            {
                string resouceType = buildArtifact.Resource.Type;
                executionContext.Warning(StringUtil.Loc("RMArtifactTypeNotSupported", resouceType));
            }
        }
    }
}