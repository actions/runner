using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseDefinitionToFolderMap
    {
        [JsonProperty("ReleaseDirectory")]
        public string ReleaseDirectory { get; set; }
    }
}