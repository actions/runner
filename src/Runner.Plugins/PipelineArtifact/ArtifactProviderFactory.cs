using GitHub.Build.WebApi;
using GitHub.Services.WebApi;
using System;
using GitHub.Runner.Sdk;
using GitHub.Services.Content.Common.Tracing;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    internal class ArtifactProviderFactory
    {
        private readonly PipelineArtifactProvider pipelineArtifactProvider;

        public ArtifactProviderFactory(RunnerActionPluginExecutionContext context, VssConnection connection, CallbackAppTraceSource tracer)
        {
            pipelineArtifactProvider = new PipelineArtifactProvider(context, connection, tracer);
        }

        public IArtifactProvider GetProvider(BuildArtifact buildArtifact)
        {
            IArtifactProvider provider;
            string artifactType = buildArtifact.Resource.Type;
            switch (artifactType)
            {
                case PipelineArtifactConstants.PipelineArtifact:
                    provider = pipelineArtifactProvider;
                    break;
                default:
                    throw new InvalidOperationException($"{buildArtifact} is not PipelineArtifact");
            }
            return provider;
        }
    }
}
