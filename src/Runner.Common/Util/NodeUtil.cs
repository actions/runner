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

        /// <summary>
        /// Checks if Node24 is requested but running on ARM32 Linux, and determines if fallback is needed.
        /// </summary>
        /// <param name="preferredVersion">The preferred Node version</param>
        /// <returns>A tuple containing the adjusted node version and an optional warning message</returns>
        public static (string nodeVersion, string warningMessage) CheckNodeVersionForLinuxArm32(string preferredVersion)
        {
            if (string.Equals(preferredVersion, "node24", StringComparison.OrdinalIgnoreCase) &&
                Constants.Runner.PlatformArchitecture.Equals(Constants.Architecture.Arm) &&
                Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                return ("node20", "Node 24 is not supported on Linux ARM32 platforms. Falling back to Node 20.");
            }

            return (preferredVersion, null);
        }
    }
}
