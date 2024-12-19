using System;
using System.Collections.ObjectModel;

namespace GitHub.Runner.Common.Util
{
    public static class NodeUtil
    {
        private const string _defaultNodeVersion = "node20";
        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(new[] { "node20" });
        public static string GetInternalNodeVersion()
        {
            var forcedInternalNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion);
            var isForcedInternalNodeVersion = !string.IsNullOrEmpty(forcedInternalNodeVersion) && BuiltInNodeVersions.Contains(forcedInternalNodeVersion);

            if (isForcedInternalNodeVersion)
            {
                return forcedInternalNodeVersion;
            }
            return _defaultNodeVersion;
        }
    }
}
