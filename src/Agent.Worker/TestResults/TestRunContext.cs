namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestRunContext
    {
        public TestRunContext(string owner, string platform, string configuration, int buildId, string buildUri, string releaseUri, string releaseEnvironmentUri, string runName = null)
        {
            this.Owner = owner;
            this.Platform = platform;
            this.Configuration = configuration;
            this.BuildId = buildId;
            this.BuildUri = buildUri;
            this.ReleaseUri = releaseUri;
            this.ReleaseEnvironmentUri = releaseEnvironmentUri;
            this.RunName = runName;
        }

        public string Owner { get; }
        public string Configuration { get; }
        public string Platform { get; }
        public int BuildId { get; }
        public string BuildUri { get; }
        public string ReleaseUri { get; }
        public string ReleaseEnvironmentUri { get; }
        public string RunName { get; set; }
    }
}