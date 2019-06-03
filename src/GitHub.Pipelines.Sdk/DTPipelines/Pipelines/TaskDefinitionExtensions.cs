using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TaskDefinitionExtensions
    {
        public static String ComputeDisplayName(
            this TaskDefinition taskDefinition, 
            IDictionary<String, String> inputs)
        {
            if (!String.IsNullOrEmpty(taskDefinition.InstanceNameFormat))
            {
                return VariableUtility.ExpandVariables(taskDefinition.InstanceNameFormat, inputs);
            }
            else if (!String.IsNullOrEmpty(taskDefinition.FriendlyName))
            {
                return taskDefinition.FriendlyName;
            }
            else
            {
                return taskDefinition.Name;
            }
        }

        /// <summary>
        /// Returns the maximum of the two versions: the currentMinimum and the task's MinimumAgentVersion
        /// </summary>
        public static String GetMinimumAgentVersion(
            this TaskDefinition taskDefinition, 
            String currentMinimum)
        {
            String minimumVersion;

            // If task.minAgentVersion > currentMin, this task needs a newer agent. So, return task.minAgentVersion
            if (DemandMinimumVersion.CompareVersion(taskDefinition.MinimumAgentVersion, currentMinimum) > 0)
            {
                minimumVersion = taskDefinition.MinimumAgentVersion;
            }
            else
            {
                minimumVersion = currentMinimum;
            }

            // If any of the task execution jobs requires Node10, return the minimum agent version that supports it
            if (taskDefinition.RequiresNode10() &&
                DemandMinimumVersion.CompareVersion(s_node10MinAgentVersion, minimumVersion) > 0)
            {
                minimumVersion = s_node10MinAgentVersion;
            }

            return minimumVersion;
        }

        private static bool RequiresNode10(
            this TaskDefinition taskDefinition)
        {
            return taskDefinition.PreJobExecution.Keys.Contains(s_node10, StringComparer.OrdinalIgnoreCase) ||
                taskDefinition.Execution.Keys.Contains(s_node10, StringComparer.OrdinalIgnoreCase) ||
                taskDefinition.PostJobExecution.Keys.Contains(s_node10, StringComparer.OrdinalIgnoreCase);
        }

        private static string s_node10MinAgentVersion = "2.144.0";
        private static string s_node10 = "Node10";
    }
}
