namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class TfsGitArtifactDetails : IArtifactDetails
    {
        public string RelativePath { get; set; }

        public string ProjectId { get; set; }

        public string RepositoryId { get; set; }

        public string Branch { get; set; }

        public string CheckoutSubmodules { get; set; }

        public string GitLfsSupport { get; set; }

        public string FetchDepth { get; set; }
    }
}