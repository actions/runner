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
            var buildDropManager = this.GetBDM(context, connection);

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
        internal async Task DownloadAsync(
            AgentTaskPluginExecutionContext context,
            Guid projectId,
            int buildId,
            string artifactName,
            string targetDir,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;

            // 1) get manifest id from artifact data
            BuildServer buildHelper = new BuildServer(connection);
            BuildArtifact art = await buildHelper.GetArtifact(projectId, buildId, artifactName, cancellationToken);
            if (art.Resource.Type != "PipelineArtifact")
            {
                throw new ArgumentException("The artifact is not of the type Pipeline Artifact\n");
            }
            var manifestId = DedupIdentifier.Create(art.Resource.Data);
            
            // 2) download to the target path
            var buildDropManager = this.GetBDM(context, connection);
            await buildDropManager.DownloadAsync(manifestId, targetDir, cancellationToken);
        }

        // Download with minimatch patterns.
        internal async Task DownloadAsyncMinimatch(
            AgentTaskPluginExecutionContext context,
            Guid projectId,
            int buildId,
            string artifactName,
            string targetDir,
            string[] minimatchFilters,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;

            // 1) get manifest id from artifact data
            BuildServer buildHelper = new BuildServer(connection);
            BuildArtifact art = await buildHelper.GetArtifact(projectId, buildId, artifactName, cancellationToken);
            if (art.Resource.Type != "PipelineArtifact")
            {
                throw new ArgumentException($"The artifact is not of the type Pipeline Artifact. Unrecognized type: {art.Resource.Type}.");
            }
            var manifestId = DedupIdentifier.Create(art.Resource.Data);

            // 2) download to the target path
            var buildDropManager = this.GetBDM(context, connection);
            DownloadPipelineArtifactOptions options = DownloadPipelineArtifactOptions.CreateWithManifestId(manifestId, targetDir, proxyUri: null, minimatchPatterns: minimatchFilters);
            await buildDropManager.DownloadAsync(options, cancellationToken);
        }

        // Download pipeline artifact with project name.
        internal async Task DownloadAsyncWithProjectNameMiniMatch(
            AgentTaskPluginExecutionContext context,
            string project,
            int buildId,
            string artifactName,
            string targetDir,
            string[] minimatchFilters,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;

            // 1) get manifest id from artifact data
            BuildServer buildHelper = new BuildServer(connection);
            BuildArtifact art = await buildHelper.GetArtifactWithProjectAsync(project, buildId, artifactName, cancellationToken);
            if (art.Resource.Type != "PipelineArtifact")
            {
                throw new ArgumentException("The artifact is not of the type Pipeline Artifact\n");
            }
            var manifestId = DedupIdentifier.Create(art.Resource.Data);

            // 2) download to the target path
            var buildDropManager = this.GetBDM(context, connection);
            DownloadPipelineArtifactOptions options = DownloadPipelineArtifactOptions.CreateWithManifestId(manifestId, targetDir, proxyUri: null, minimatchPatterns: minimatchFilters);
            await buildDropManager.DownloadAsync(options, cancellationToken);
        }

        private BuildDropManager GetBDM(AgentTaskPluginExecutionContext context, VssConnection connection)
        {
            var dedupStoreHttpClient = connection.GetClient<DedupStoreHttpClient>();
            var tracer = new CallbackAppTraceSource(str => context.Output(str), System.Diagnostics.SourceLevels.Information);
            dedupStoreHttpClient.SetTracer(tracer);
            var client = new DedupStoreClientWithDataport(dedupStoreHttpClient, 16 * Environment.ProcessorCount);
            var buildDropManager = new BuildDropManager(client, tracer);
            return buildDropManager;
        }
    }
}