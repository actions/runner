using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Agent.Plugins.PipelineArtifact;
using Agent.Sdk;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.BlobStore.Common;
using Microsoft.VisualStudio.Services.BlobStore.WebApi;
using Microsoft.VisualStudio.Services.Content.Common;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Content.Common.Tracing;
using Microsoft.VisualStudio.Services.PipelineCache.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace Agent.Plugins.PipelineCache
{    
    public class PipelineCacheServer
    {
        public const string RootId = "RootId";
        public const string ProofNodes = "ProofNodes";
        private const string PipelineCacheVarPrefix = "PipelineCache";

        internal async Task UploadAsync(
            AgentTaskPluginExecutionContext context,
            IEnumerable<string> key,
            string path,
            string salt,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;
            DedupManifestArtifactClient dedupManifestClient = DedupManifestArtifactClientFactory.CreateDedupManifestClient(context, connection);

            var result = await dedupManifestClient.PublishAsync(path, cancellationToken);
            var scope = "myscope";

            throw new NotImplementedException();
            // CreatePipelineCacheArtifactOptions options = new CreatePipelineCacheArtifactOptions
            // {
            //     Key = key,
            //     RootId = result.RootId,
            //     ManifestId = result.ManifestId,
            //     Scope = scope,
            //     ProofNodes = result.ProofNodes.ToArray(),
            //     Salt = salt
            // };

            // var pipelineCacheClient = this.CreateClient(context, connection);
            // await pipelineCacheClient.CreatePipelineCacheArtifactAsync(options, cancellationToken);

            // Console.WriteLine("Saved item.");
        }

        internal async Task DownloadAsync(
            AgentTaskPluginExecutionContext context,
            IEnumerable<string> key,
            string path,
            string salt,
            string variableToSetOnHit,
            CancellationToken cancellationToken)
        {
            VssConnection connection = context.VssConnection;
            var pipelineCacheClient = this.CreateClient(context, connection);

            throw new NotImplementedException();
            // GetPipelineCacheArtifactOptions options = new GetPipelineCacheArtifactOptions
            // {
            //     Key = key,
            //     Scope = "myscope",
            //     Salt = salt,
            // };

            // var result = await pipelineCacheClient.GetPipelineCacheArtifactAsync(options, cancellationToken);
            // if (result == null)
            // {
            //     return;
            // }
            // else
            // {
            //     Console.WriteLine("Manifest ID is: {0}", result.ManifestId.ValueString);
            //     DedupManifestArtifactClient dedupManifestClient = DedupManifestArtifactClientFactory.CreateDedupManifestClient(context, connection);
            //     await this.DownloadPipelineCacheAsync(dedupManifestClient, result.ManifestId, path, cancellationToken);
            //     context.SetVariable($"{PipelineCacheVarPrefix}.{variableToSetOnHit}", "True");
            //     Console.WriteLine("Cache restored.");
            // }
        }

        private PipelineCacheClient CreateClient(
            AgentTaskPluginExecutionContext context,
            VssConnection connection)
        {
            var tracer = new CallbackAppTraceSource(str => context.Output(str), System.Diagnostics.SourceLevels.Information);
            IClock clock = UtcClock.Instance;           
            var pipelineCacheHttpClient = connection.GetClient<PipelineCacheHttpClient>();
            var pipelineCacheClient = new PipelineCacheClient(pipelineCacheHttpClient, clock, tracer);

            return pipelineCacheClient;
        }

        private Task DownloadPipelineCacheAsync(
            DedupManifestArtifactClient dedupManifestClient,
            DedupIdentifier manifestId,
            string targetDirectory,
            CancellationToken cancellationToken)
        {
            DownloadPipelineArtifactOptions options = DownloadPipelineArtifactOptions.CreateWithManifestId(
                manifestId,
                targetDirectory,
                proxyUri: null,
                minimatchPatterns: null);
            return dedupManifestClient.DownloadAsync(options, cancellationToken);
        }
    }
}