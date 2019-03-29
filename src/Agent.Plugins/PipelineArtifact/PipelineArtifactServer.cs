using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.BlobStore.Common;
using Microsoft.VisualStudio.Services.Content.Common.Tracing;
using Microsoft.VisualStudio.Services.BlobStore.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using Agent.Sdk;

namespace Agent.Plugins.PipelineArtifact
{    
    // A wrapper of BuildDropManager, providing basic functionalities such as uploading and downloading pipeline artifacts.
    public class PipelineArtifactServer
    {
        public static readonly string RootId = "RootId";
        public static readonly string ProofNodes = "ProofNodes";
        public const string PipelineArtifactTypeName = "PipelineArtifact";

        // Upload from target path to VSTS BlobStore service through BuildDropManager, then associate it with the build
        internal async Task UploadAsync(
            AgentTaskPluginExecutionContext context,
            Guid projectId,
            int buildId,
            string name,
            string source,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;
            var buildDropManager = this.CreateBulidDropManager(context, connection);

            //Upload the pipeline artifact.
            var result = await buildDropManager.PublishAsync(source, cancellationToken);

            // 2) associate the pipeline artifact with an build artifact
            BuildServer buildHelper = new BuildServer(connection);
            Dictionary<string, string> propertiesDictionary = new Dictionary<string, string>();
            propertiesDictionary.Add(RootId, result.RootId.ValueString);
            propertiesDictionary.Add(ProofNodes, StringUtil.ConvertToJson(result.ProofNodes.ToArray()));
            var artifact = await buildHelper.AssociateArtifact(projectId, buildId, name, ArtifactResourceTypes.PipelineArtifact, result.ManifestId.ValueString, propertiesDictionary, cancellationToken);
            context.Output(StringUtil.Loc("AssociateArtifactWithBuild", artifact.Id, buildId));
        }

        // Download pipeline artifact from VSTS BlobStore service through BuildDropManager to a target path
        // Old V0 function
        internal Task DownloadAsync(
            AgentTaskPluginExecutionContext context,
            Guid projectId,
            int buildId,
            string artifactName,
            string targetDir,
            CancellationToken cancellationToken)
        {
            var downloadParameters = new PipelineArtifactDownloadParameters
            {
                ProjectRetrievalOptions = BuildArtifactRetrievalOptions.RetrieveByProjectId,
                ProjectId = projectId,
                BuildId = buildId,
                ArtifactName = artifactName,
                TargetDirectory = targetDir
            };

            return this.DownloadAsync(context, downloadParameters, DownloadOptions.SingleDownload, cancellationToken);
        }

        // Download with minimatch patterns.
        internal async Task DownloadAsync(
            AgentTaskPluginExecutionContext context,
            PipelineArtifactDownloadParameters downloadParameters,
            DownloadOptions downloadOptions, 
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;
            var buildDropManager = this.CreateBulidDropManager(context, connection);
            BuildServer buildHelper = new BuildServer(connection);
            
            // download all pipeline artifacts if artifact name is missing
            if (downloadOptions == DownloadOptions.MultiDownload)
            {
                List<BuildArtifact> artifacts;
                if (downloadParameters.ProjectRetrievalOptions == BuildArtifactRetrievalOptions.RetrieveByProjectId)
                {
                    artifacts = await buildHelper.GetArtifactsAsync(downloadParameters.ProjectId, downloadParameters.BuildId, cancellationToken);
                }
                else if (downloadParameters.ProjectRetrievalOptions == BuildArtifactRetrievalOptions.RetrieveByProjectName)
                {
                    if (string.IsNullOrEmpty(downloadParameters.ProjectName))
                    {
                        throw new InvalidOperationException("Project name can't be empty when trying to fetch build artifacts!");
                    }
                    else
                    {
                        artifacts = await buildHelper.GetArtifactsWithProjectNameAsync(downloadParameters.ProjectName, downloadParameters.BuildId, cancellationToken);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unreachable code!");
                }

                IEnumerable<BuildArtifact> pipelineArtifacts = artifacts.Where(a => a.Resource.Type == PipelineArtifactTypeName);
                if (pipelineArtifacts.Count() == 0)
                {
                    throw new ArgumentException("Could not find any pipeline artifacts in the build.");
                }
                else
                {
                    context.Output(StringUtil.Loc("DownloadingMultiplePipelineArtifacts", pipelineArtifacts.Count()));

                    var artifactNameAndManifestIds = pipelineArtifacts.ToDictionary(
                        keySelector: (a) => a.Name, // keys should be unique, if not something is really wrong
                        elementSelector: (a) => DedupIdentifier.Create(a.Resource.Data));
                    // 2) download to the target path
                    var options = DownloadPipelineArtifactOptions.CreateWithMultiManifestIds(
                        artifactNameAndManifestIds,
                        downloadParameters.TargetDirectory,
                        proxyUri: null,
                        minimatchPatterns: downloadParameters.MinimatchFilters);
                    await buildDropManager.DownloadAsync(options, cancellationToken);                        
                }
            }
            else if (downloadOptions == DownloadOptions.SingleDownload)
            {
                // 1) get manifest id from artifact data
                BuildArtifact buildArtifact;
                if (downloadParameters.ProjectRetrievalOptions == BuildArtifactRetrievalOptions.RetrieveByProjectId)
                {
                    buildArtifact = await buildHelper.GetArtifact(downloadParameters.ProjectId, downloadParameters.BuildId, downloadParameters.ArtifactName, cancellationToken);
                }
                else if (downloadParameters.ProjectRetrievalOptions == BuildArtifactRetrievalOptions.RetrieveByProjectName)
                {
                    if (string.IsNullOrEmpty(downloadParameters.ProjectName))
                    {
                        throw new InvalidOperationException("Project name can't be empty when trying to fetch build artifacts!");
                    }
                    else
                    {
                        buildArtifact = await buildHelper.GetArtifactWithProjectNameAsync(downloadParameters.ProjectName, downloadParameters.BuildId, downloadParameters.ArtifactName, cancellationToken);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unreachable code!");
                }

                var manifestId = DedupIdentifier.Create(buildArtifact.Resource.Data);
                var options = DownloadPipelineArtifactOptions.CreateWithManifestId(
                    manifestId,
                    downloadParameters.TargetDirectory,
                    proxyUri: null,
                    minimatchPatterns: downloadParameters.MinimatchFilters);

                await buildDropManager.DownloadAsync(options, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Unreachable code!");
            }
        }

        private BuildDropManager CreateBulidDropManager(AgentTaskPluginExecutionContext context, VssConnection connection)
        {
            var dedupStoreHttpClient = connection.GetClient<DedupStoreHttpClient>();
            var tracer = new CallbackAppTraceSource(str => context.Output(str), System.Diagnostics.SourceLevels.Information);
            dedupStoreHttpClient.SetTracer(tracer);
            var client = new DedupStoreClientWithDataport(dedupStoreHttpClient, 16 * Environment.ProcessorCount);
            var buildDropManager = new BuildDropManager(client, tracer);
            return buildDropManager;
        }
    }

    internal class PipelineArtifactDownloadParameters
    {
        /// <remarks>
        /// Options on how to retrieve the build using the following parameters.
        /// </remarks>
        public BuildArtifactRetrievalOptions ProjectRetrievalOptions { get; set; }
        /// <remarks>
        /// Either project ID or project name need to be supplied.
        /// </remarks>
        public Guid ProjectId { get; set; }
        /// <remarks>
        /// Either project ID or project name need to be supplied.
        /// </remarks>
        public string ProjectName { get; set; }
        public int BuildId { get; set; }
        public string ArtifactName { get; set; }
        public string TargetDirectory { get; set; }
        public string[] MinimatchFilters { get; set; }
    }

    internal enum BuildArtifactRetrievalOptions
    {
        RetrieveByProjectId,
        RetrieveByProjectName
    }

    internal enum DownloadOptions
    {
        SingleDownload,        
        MultiDownload
    }
}