namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class CustomArtifactDownloadDetails
    {
        public string Name { get; set; }

        public string DownloadUrl { get; set; }

        public string FileShareLocation { get; set; }

        public string StreamType { get; set; }

        public string RelativePath { get; set; }
    }
}