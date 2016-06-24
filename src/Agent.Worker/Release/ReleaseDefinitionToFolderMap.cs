using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseDefinitionToFolderMap
    {
        [JsonProperty("releaseDirectory")]
        public string ReleaseDirectory { get; set; }
    }
}