using System;
using System.Collections.ObjectModel;

namespace GitHub.Runner.Common.Util
{

    public static class NodeUtil
    {
        private const string _defaultNodeVersion = "node16";
        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(new[] {"node12", "node16"});
        public static string GetNodeVersion()
        {
            var forcedNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedNodeVersion);
            return !string.IsNullOrEmpty(forcedNodeVersion) && BuiltInNodeVersions.Contains(forcedNodeVersion) ? forcedNodeVersion : _defaultNodeVersion;
        }
    }
}
