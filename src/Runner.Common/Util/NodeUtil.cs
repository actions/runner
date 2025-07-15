using System;
using System.Collections.ObjectModel;

namespace GitHub.Runner.Common.Util
{
    public static class NodeUtil
    {
        private const string _defaultNodeVersion = "node20";
        private const string _node24Version = "node24";
        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(new[] { "node20", "node24" });
        public static string GetInternalNodeVersion()
        {
            var forcedInternalNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion);
            var isForcedInternalNodeVersion = !string.IsNullOrEmpty(forcedInternalNodeVersion) && BuiltInNodeVersions.Contains(forcedInternalNodeVersion);

            if (isForcedInternalNodeVersion)
            {
                return forcedInternalNodeVersion;
            }

            var useNode24FlagValue = Environment.GetEnvironmentVariable(Constants.Runner.Features.UseNode24) ?? 
                                     Environment.GetEnvironmentVariable("RUNNER_USENODE24") ??
                                     Environment.GetEnvironmentVariable("runner_usenode24");
                                     
            var useNode24 = !string.IsNullOrEmpty(useNode24FlagValue) && 
                           (string.Equals(useNode24FlagValue, "true", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(useNode24FlagValue, "1", StringComparison.OrdinalIgnoreCase));
                            
            if (useNode24)
            {
                return _node24Version;
            }
            
            return _defaultNodeVersion;
        }
    }
}
