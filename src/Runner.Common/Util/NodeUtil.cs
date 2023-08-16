using System;
using System.Collections.ObjectModel;

namespace GitHub.Runner.Common.Util
{
    public static class NodeUtil
    {
        private const string _defaultNodeVersion = "node16";

        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX) && System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? new[] { "node16", "node20" } : new[] { "node12", "node16", "node20" });

        public static string GetInternalNodeVersion()
        {
            var forcedNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion);
            return !string.IsNullOrEmpty(forcedNodeVersion) && BuiltInNodeVersions.Contains(forcedNodeVersion) ? forcedNodeVersion : _defaultNodeVersion;
        }
    }
}
