using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class GitHubArtifactDetails : IArtifactDetails
    {
        public string RelativePath { get; set; }

        public Uri CloneUrl { get; set; }

        public string Branch { get; set; }

        public string ConnectionName { get; set; }

        public string CheckoutSubmodules { get; set; }

        public string GitLfsSupport { get; set; }

        public string FetchDepth { get; set; }
    }
}