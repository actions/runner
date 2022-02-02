namespace GitHub.Runner.Common.Util
{
    using System;
    using System.Collections.ObjectModel;

    public static class NodeUtil
    {
        public const string LatestNodeVersion = "node16";
        public static readonly ReadOnlyCollection<string> AllowedNodeVersions = new(new[] {"node12", "node16"});
        public static string GetForcedOrLatestNodeVersion()
        {
            var forcedNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedNodeVersion);
            return string.IsNullOrEmpty(forcedNodeVersion) ? LatestNodeVersion : forcedNodeVersion;
        }
    }
}
