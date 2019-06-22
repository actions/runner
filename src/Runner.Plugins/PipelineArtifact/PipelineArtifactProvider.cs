using GitHub.Build.WebApi;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.BlobStore.WebApi;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using System.Threading;
using System.Linq;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    internal class PipelineArtifactProvider : IArtifactProvider
    {
        // Old default for hosted agents was 16*2 cores = 32. 
        // In my tests of a node_modules folder, this 32x parallelism was consistently around 47 seconds.
        // At 192x it was around 16 seconds and 256x was no faster.
        private const int DefaultDedupStoreClientMaxParallelism = 192;

        internal static int GetDedupStoreClientMaxParallelism(RunnerActionPluginExecutionContext context) {
            int parallelism = DefaultDedupStoreClientMaxParallelism;
            if(context.Variables.TryGetValue("AZURE_PIPELINES_DEDUP_PARALLELISM", out VariableValue v)) {
                if (!int.TryParse(v.Value, out parallelism)) {
                    context.Info($"Could not parse the value of AZURE_PIPELINES_DEDUP_PARALLELISM, '{v.Value}', as an integer. Defaulting to {DefaultDedupStoreClientMaxParallelism}");
                    parallelism = DefaultDedupStoreClientMaxParallelism;
                }
            }
            context.Info(string.Format("Dedup parallelism: {0}", parallelism));
            return parallelism;
        } 

        private readonly BuildDropManager buildDropManager;
        private readonly CallbackAppTraceSource tracer;

        public PipelineArtifactProvider(RunnerActionPluginExecutionContext context, VssConnection connection, CallbackAppTraceSource tracer)
        {
            var dedupStoreHttpClient = connection.GetClient<DedupStoreHttpClient>();
            this.tracer = tracer;
            dedupStoreHttpClient.SetTracer(tracer);
            int parallelism = GetDedupStoreClientMaxParallelism(context);
            var client = new DedupStoreClientWithDataport(dedupStoreHttpClient, parallelism);
            buildDropManager = new BuildDropManager(client, this.tracer);
        }

        public async Task DownloadSingleArtifactAsync(PipelineArtifactDownloadParameters downloadParameters, BuildArtifact buildArtifact, CancellationToken cancellationToken)
        {
            var manifestId = DedupIdentifier.Create(buildArtifact.Resource.Data);
            var options = DownloadDedupManifestArtifactOptions.CreateWithManifestId(
                manifestId,
                downloadParameters.TargetDirectory,
                proxyUri: null,
                minimatchPatterns: downloadParameters.MinimatchFilters);
            await buildDropManager.DownloadAsync(options, cancellationToken);
        }

        public async Task DownloadMultipleArtifactsAsync(PipelineArtifactDownloadParameters downloadParameters, IEnumerable<BuildArtifact> buildArtifacts, CancellationToken cancellationToken)
        {
            var artifactNameAndManifestIds = buildArtifacts.ToDictionary(
                keySelector: (a) => a.Name, // keys should be unique, if not something is really wrong
                elementSelector: (a) => DedupIdentifier.Create(a.Resource.Data));
            // 2) download to the target path
            var options = DownloadDedupManifestArtifactOptions.CreateWithMultiManifestIds(
                artifactNameAndManifestIds,
                downloadParameters.TargetDirectory,
                proxyUri: null,
                minimatchPatterns: downloadParameters.MinimatchFilters,
                minimatchFilterWithArtifactName: downloadParameters.MinimatchFilterWithArtifactName);
            await buildDropManager.DownloadAsync(options, cancellationToken);
        }
    }
}
