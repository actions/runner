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

namespace Agent.Plugins.BuildDrop
{    
    // A wrapper of BuildDropManager, providing basic functionalities such as uploading and downloading drop artifacts.
    public class BuildDropServer
    {
        public static readonly string RootId = "RootId";
        public static readonly string ProofNodes = "ProofNodes";

        // Upload from target path to VSTS BlobStore service through BuildDropManager, then associate it with the build
        internal async Task UploadDropArtifactAsync(
            AgentTaskPluginExecutionContext context,
            Guid projectId,
            int buildId,
            string name,
            string source,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;

            // 1) upload build drop to VSTS BlobStore
            var httpclient = connection.GetClient<DedupStoreHttpClient>();
            var tracer = new CallbackAppTraceSource(str => context.Output(str), System.Diagnostics.SourceLevels.Information);
            httpclient.SetTracer(tracer);
            var client = new DedupStoreClientWithDataport(httpclient, 16 * Environment.ProcessorCount);
            var buildDropManager = new BuildDropManager(client, tracer);
            var result = await buildDropManager.PublishAsync(source, cancellationToken);

            // 2) associate the drop with an artifact for this build
            BuildServer buildHelper = new BuildServer(connection);
            Dictionary<string, string> propertiesDictionary = new Dictionary<string, string>();
            propertiesDictionary.Add(RootId, result.RootId.ValueString);
            propertiesDictionary.Add(ProofNodes, StringUtil.ConvertToJson(result.ProofNodes.ToArray()));
            var artifact = await buildHelper.AssociateArtifact(projectId, buildId, name, ArtifactResourceTypes.Drop, result.ManifestId.ValueString, propertiesDictionary, cancellationToken);
            context.Output(StringUtil.Loc("AssociateArtifactWithBuild", artifact.Id, buildId));
        }

        // Download drop artifact from VSTS BlobStore service through BuildDropManager to a target path
        internal async Task DownloadDropArtifactAsync(
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
            var manifestId = DedupIdentifier.Create(art.Resource.Data);

            // 2) download to the target path
            var httpclient = connection.GetClient<DedupStoreHttpClient>();
            var tracer = new CallbackAppTraceSource(str => context.Output(str), System.Diagnostics.SourceLevels.Information);
            httpclient.SetTracer(tracer);
            var client = new DedupStoreClientWithDataport(httpclient, maxParallelism: 16 * Environment.ProcessorCount);
            var buildDropManager = new BuildDropManager(client, tracer);
            await buildDropManager.DownloadAsync(manifestId, targetDir, cancellationToken);
        }
    }
}