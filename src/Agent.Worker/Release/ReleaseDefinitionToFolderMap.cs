using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseDefinitionToFolderMap
    {
        [JsonProperty("release_artifactsdirectory")]
        public string ArtifactsDirectory { get; set; }
    }
}