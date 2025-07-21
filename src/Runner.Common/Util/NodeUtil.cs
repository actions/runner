using System;
using System.Collections.Generic;
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
        /// Determines the appropriate Node version for Actions to use
        /// </summary>
        /// <param name="workflowEnvironment">Optional dictionary containing workflow-level environment variables</param>
        /// <param name="useNode24ByDefault">Feature flag indicating if Node 24 should be the default</param>
        /// <param name="requireNode24">Feature flag indicating if Node 24 is required</param>
        /// <param name="warningCallback">Optional callback for emitting warnings</param>
        /// <returns>The Node version to use (node20 or node24) and warning message if both env vars are set</returns>
        public static (string nodeVersion, string warningMessage) DetermineActionsNodeVersion(
            IDictionary<string, string> workflowEnvironment = null,
            bool useNode24ByDefault = false,
            bool requireNode24 = false)
        {
            bool forceNode24 = IsEnvironmentVariableTrue(Constants.Runner.NodeMigration.ForceNode24Variable, workflowEnvironment);
            bool allowUnsecureNode = IsEnvironmentVariableTrue(Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, workflowEnvironment);
            
            string warningMessage = null;
            if (forceNode24 && allowUnsecureNode)
            {
                string defaultVersion = useNode24ByDefault ? Constants.Runner.NodeMigration.Node24 : Constants.Runner.NodeMigration.Node20;
                warningMessage = $"Both {Constants.Runner.NodeMigration.ForceNode24Variable} and {Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable} environment variables are set to true. This is likely a configuration error. Using the default Node version: {defaultVersion}.";
            }
            
            // Phase 3: If require Node 24 flag is enabled, always use Node 24 regardless of environment variables
            // Unless allowUnsecureNode is set (highest precedence)
            if (requireNode24)
            {
                if (allowUnsecureNode)
                {
                    return (Constants.Runner.NodeMigration.Node20, warningMessage);
                }
                return (Constants.Runner.NodeMigration.Node24, warningMessage);
            }
            
            // If both environment variables are set, use the default for the current phase
            if (forceNode24 && allowUnsecureNode)
            {
                return (useNode24ByDefault ? Constants.Runner.NodeMigration.Node24 : Constants.Runner.NodeMigration.Node20, warningMessage);
            }
            
            // Phase 2: If Node 24 is the default (flag enabled)
            if (useNode24ByDefault)
            {
                if (allowUnsecureNode)
                {
                    return (Constants.Runner.NodeMigration.Node20, warningMessage);
                }
                
                // The forceNode24 check is redundant here since the default is already Node24,
                // but we're keeping it for code clarity
                return (Constants.Runner.NodeMigration.Node24, warningMessage);
            }
            
            // Phase 1: Node 20 is the default
            if (forceNode24)
            {
                return (Constants.Runner.NodeMigration.Node24, warningMessage);
            }
            
            return (Constants.Runner.NodeMigration.Node20, warningMessage);
        }

        /// <summary>
        /// Checks if Node24 is requested but running on ARM32 Linux, and determines if fallback is needed.
        /// </summary>
        /// <param name="preferredVersion">The preferred Node version</param>
        /// <returns>A tuple containing the adjusted node version and an optional warning message</returns>
        public static (string nodeVersion, string warningMessage) CheckNodeVersionForLinuxArm32(string preferredVersion)
        {
            if (string.Equals(preferredVersion, Constants.Runner.NodeMigration.Node24, StringComparison.OrdinalIgnoreCase) &&
                Constants.Runner.PlatformArchitecture.Equals(Constants.Architecture.Arm) &&
                Constants.Runner.Platform.Equals(Constants.OSPlatform.Linux))
            {
                return (Constants.Runner.NodeMigration.Node20, "Node 24 is not supported on Linux ARM32 platforms. Falling back to Node 20.");
            }

            return (preferredVersion, null);
        }
        
        /// <summary>
        /// Checks if an environment variable is set to "true" in either the workflow environment or system environment
        /// </summary>
        /// <param name="variableName">The name of the environment variable</param>
        /// <param name="workflowEnvironment">Optional dictionary containing workflow-level environment variables</param>
        /// <returns>True if the variable is set to "true" in either environment</returns>
        private static bool IsEnvironmentVariableTrue(string variableName, IDictionary<string, string> workflowEnvironment)
        {
            if (workflowEnvironment != null && workflowEnvironment.TryGetValue(variableName, out string workflowValue))
            {
                return string.Equals(workflowValue, "true", StringComparison.OrdinalIgnoreCase);
            }
            
            string systemValue = Environment.GetEnvironmentVariable(variableName);
            return string.Equals(systemValue, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
