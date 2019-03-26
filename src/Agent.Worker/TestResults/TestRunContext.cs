namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestRunContext
    {
        public TestRunContext(string owner, string platform, string configuration, int buildId, string buildUri, string releaseUri, string releaseEnvironmentUri, string runName = null, string pullRequestTargetBranchName = null)
        {
            Owner = owner;
            Platform = platform;
            Configuration = configuration;
            BuildId = buildId;
            BuildUri = buildUri;
            ReleaseUri = releaseUri;
            ReleaseEnvironmentUri = releaseEnvironmentUri;
            RunName = runName;
            PullRequestTargetBranchName = pullRequestTargetBranchName;
        }

        public string Owner { get; }
        public string Configuration { get; }
        public string Platform { get; }
        public int BuildId { get; }
        public string BuildUri { get; }
        public string ReleaseUri { get; }
        public string ReleaseEnvironmentUri { get; }
        public string RunName { get; set; }
        public string PullRequestTargetBranchName { get; set; }
    }
}