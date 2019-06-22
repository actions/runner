namespace GitHub.Services.BlobStore.Common
{
    /// <summary>
    /// Holds the monikers for the various scopes/experiences that eventually get
    /// hardened in our BS metadata tables.
    /// </summary>
    public sealed class ArtifactScopeConstants
    {
        public const string PipelineArtifact = "pipelineartifact";

        public const string PipelineCaching = "pipelinecaching";
    }
}
