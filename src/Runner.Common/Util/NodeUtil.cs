using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Util
{
    /// <summary>
    /// Represents details about an environment variable, including its value and source
    /// </summary>
    public class EnvironmentVariableInfo
    {
        /// <summary>
        /// Gets or sets whether the value evaluates to true
        /// </summary>
        public bool IsTrue { get; set; }

        /// <summary>
        /// Gets or sets whether the value came from the workflow environment
        /// </summary>
        public bool FromWorkflow { get; set; }

        /// <summary>
        /// Gets or sets whether the value came from the system environment
        /// </summary>
        public bool FromSystem { get; set; }

        /// <summary>
        /// Gets or sets the raw string value of the environment variable
        /// </summary>
        public string RawValue { get; set; }
    }

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
        /// <returns>The Node version to use (node20 or node24) and warning message if both env vars are set</returns>
        public static (string nodeVersion, string warningMessage) DetermineActionsNodeVersion(
            IDictionary<string, string> workflowEnvironment = null,
            bool useNode24ByDefault = false,
            bool requireNode24 = false)
        {
            // Get environment variable details with source information
            var forceNode24Details = GetEnvironmentVariableDetails(
                Constants.Runner.NodeMigration.ForceNode24Variable, workflowEnvironment);

            var allowUnsecureNodeDetails = GetEnvironmentVariableDetails(
                Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, workflowEnvironment);

            bool forceNode24 = forceNode24Details.IsTrue;
            bool allowUnsecureNode = allowUnsecureNodeDetails.IsTrue;
            string warningMessage = null;

            // Phase 3: Always use Node 24 regardless of environment variables
            if (requireNode24)
            {
                return (Constants.Runner.NodeMigration.Node24, null);
            }

            // Check if both flags are set from the same source
            bool bothFromWorkflow = forceNode24Details.IsTrue && allowUnsecureNodeDetails.IsTrue &&
                                   forceNode24Details.FromWorkflow && allowUnsecureNodeDetails.FromWorkflow;

            bool bothFromSystem = forceNode24Details.IsTrue && allowUnsecureNodeDetails.IsTrue &&
                                 forceNode24Details.FromSystem && allowUnsecureNodeDetails.FromSystem;

            // Handle the case when both are set in the same source
            if ((bothFromWorkflow || bothFromSystem) && !requireNode24)
            {
                string source = bothFromWorkflow ? "workflow" : "system";
                string defaultVersion = useNode24ByDefault ? Constants.Runner.NodeMigration.Node24 : Constants.Runner.NodeMigration.Node20;
                warningMessage = $"Both {Constants.Runner.NodeMigration.ForceNode24Variable} and {Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable} environment variables are set to true in the {source} environment. This is likely a configuration error. Using the default Node version: {defaultVersion}.";
                return (defaultVersion, warningMessage);
            }

            // Phase 2: Node 24 is the default
            if (useNode24ByDefault)
            {
                if (allowUnsecureNode)
                {
                    return (Constants.Runner.NodeMigration.Node20, null);
                }

                return (Constants.Runner.NodeMigration.Node24, null);
            }

            // Phase 1: Node 20 is the default
            if (forceNode24)
            {
                return (Constants.Runner.NodeMigration.Node24, null);
            }

            return (Constants.Runner.NodeMigration.Node20, null);
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
        /// Gets detailed information about an environment variable from both workflow and system environments
        /// </summary>
        /// <param name="variableName">The name of the environment variable</param>
        /// <param name="workflowEnvironment">Optional dictionary containing workflow-level environment variables</param>
        /// <returns>An EnvironmentVariableInfo object containing details about the variable from both sources</returns>
        private static EnvironmentVariableInfo GetEnvironmentVariableDetails(string variableName, IDictionary<string, string> workflowEnvironment)
        {
            var info = new EnvironmentVariableInfo();

            // Check workflow environment
            bool foundInWorkflow = false;
            string workflowValue = null;

            if (workflowEnvironment != null && workflowEnvironment.TryGetValue(variableName, out workflowValue))
            {
                foundInWorkflow = true;
                info.FromWorkflow = true;
                info.RawValue = workflowValue; // Workflow value takes precedence for the raw value
                info.IsTrue = StringUtil.ConvertToBoolean(workflowValue); // Workflow value takes precedence for the boolean value
            }

            // Also check system environment
            string systemValue = Environment.GetEnvironmentVariable(variableName);
            bool foundInSystem = !string.IsNullOrEmpty(systemValue);

            info.FromSystem = foundInSystem;

            // If not found in workflow, use system values
            if (!foundInWorkflow)
            {
                info.RawValue = foundInSystem ? systemValue : null;
                info.IsTrue = StringUtil.ConvertToBoolean(systemValue);
            }

            return info;
        }
    }
}
