namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public static class WellKnownReleaseVariables
    {
        // TODO: This is just copy from Release extension, cleanup which ever is not required. Do not use system variables from here, its already been defined in other places
        public const string System = "system";

        public const string Build = "build";

        public const string HostTypeValue = "release";

        public const string ReleaseArtifact = "release.artifacts";

        public const string ReleaseEnvironments = "release.environments";

        public const string AgentReleaseDirectory = "agent.releaseDirectory";

        public const string HostType = "system.hosttype";

        public const string ArtifactsDirectory = "system.artifactsDirectory";

        public const string CollectionId = "system.collectionId";

        public const string TeamProject = "system.teamProject";
    }
}