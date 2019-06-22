using System.Diagnostics;
using GitHub.Runner.Sdk;
using GitHub.Services.BlobStore.Common.Telemetry;
using GitHub.Services.BlobStore.WebApi;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    public static class DedupManifestArtifactClientFactory
    {
        public static DedupManifestArtifactClient CreateDedupManifestClient(RunnerActionPluginExecutionContext context, VssConnection connection, out BlobStoreClientTelemetry telemetry)
        {
            var dedupStoreHttpClient = connection.GetClient<DedupStoreHttpClient>();
            var tracer = new CallbackAppTraceSource(str => context.Output(str), SourceLevels.Information);
            dedupStoreHttpClient.SetTracer(tracer);
            var client = new DedupStoreClientWithDataport(dedupStoreHttpClient, PipelineArtifactProvider.GetDedupStoreClientMaxParallelism(context));
            return new DedupManifestArtifactClient(telemetry = new BlobStoreClientTelemetry(tracer, dedupStoreHttpClient.BaseAddress), client, tracer);
        }
    }
}