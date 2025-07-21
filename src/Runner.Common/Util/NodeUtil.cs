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
        /// <returns>The Node version to use (node20 or node24)</returns>
        public static string DetermineActionsNodeVersion(IDictionary<string, string> workflowEnvironment = null)
        {
            if (DateTime.UtcNow >= Constants.Runner.NodeMigration.Node24CutoverDate)
            {
                if (IsEnvironmentVariableTrue(Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, workflowEnvironment))
                {
                    return Constants.Runner.NodeMigration.Node20;
                }
                
                return Constants.Runner.NodeMigration.Node24;
            }
            
            if (IsEnvironmentVariableTrue(Constants.Runner.NodeMigration.ForceNode24Variable, workflowEnvironment))
            {
                return Constants.Runner.NodeMigration.Node24;
            }
            
            return Constants.Runner.NodeMigration.Node20;
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
            if (workflowEnvironment != null && 
                workflowEnvironment.TryGetValue(variableName, out string workflowValue) && 
                string.Equals(workflowValue, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            string systemValue = Environment.GetEnvironmentVariable(variableName);
            return string.Equals(systemValue, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
