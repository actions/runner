using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Utility class to perform operations on Variable groups.
    /// </summary>
    public static class VariableGroupUtility
    {
        public static VariableValue Clone(this VariableValue value)
        {
            if (keyVaultVariableType == value.GetType())
            {
                return new AzureKeyVaultVariableValue((AzureKeyVaultVariableValue)value);
            }

            return new VariableValue(value);
        }
        
        public static void PopulateVariablesAndProviderData(this VariableGroup group, String variablesJson, String providerDataJson)
        {
            switch (group.Type)
            {
                case VariableGroupType.Vsts:
                    if (variablesJson != null)
                    {
                        group.Variables = JsonUtility.FromString<IDictionary<String, VariableValue>>(variablesJson);
                    }

                    if (providerDataJson != null)
                    {
                        group.ProviderData = JsonUtility.FromString<VariableGroupProviderData>(providerDataJson);
                    }

                    break;

                case VariableGroupType.AzureKeyVault:
                    if (variablesJson != null)
                    {
                        var azureKeyVaultVariableValues = JsonUtility.FromString<IDictionary<String, AzureKeyVaultVariableValue>>(variablesJson);
                        if (azureKeyVaultVariableValues != null)
                        {
                            foreach (var azureKeyVaultVariableValue in azureKeyVaultVariableValues)
                            {
                                group.Variables[azureKeyVaultVariableValue.Key] = azureKeyVaultVariableValue.Value;
                            }
                        }
                    }

                    if (providerDataJson != null)
                    {
                        group.ProviderData = JsonUtility.FromString<AzureKeyVaultVariableGroupProviderData>(providerDataJson);
                    }

                    break;
            }
        }

        public static void PopulateVariablesAndProviderData(this VariableGroupParameters variableGroupParameters, String variablesJson, String providerDataJson)
        {
            switch (variableGroupParameters.Type)
            {
                case VariableGroupType.Vsts:
                    if (variablesJson != null)
                    {
                        variableGroupParameters.Variables = JsonUtility.FromString<IDictionary<String, VariableValue>>(variablesJson);
                    }

                    if (providerDataJson != null)
                    {
                        variableGroupParameters.ProviderData = JsonUtility.FromString<VariableGroupProviderData>(providerDataJson);
                    }

                    break;

                case VariableGroupType.AzureKeyVault:
                    if (variablesJson != null)
                    {
                        var azureKeyVaultVariableValues = JsonUtility.FromString<IDictionary<String, AzureKeyVaultVariableValue>>(variablesJson);
                        if (azureKeyVaultVariableValues != null)
                        {
                            foreach (var azureKeyVaultVariableValue in azureKeyVaultVariableValues)
                            {
                                variableGroupParameters.Variables[azureKeyVaultVariableValue.Key] = azureKeyVaultVariableValue.Value;
                            }
                        }
                    }

                    if (providerDataJson != null)
                    {
                        variableGroupParameters.ProviderData = JsonUtility.FromString<AzureKeyVaultVariableGroupProviderData>(providerDataJson);
                    }

                    break;
            }
        }

        /// <summary>
        /// Get list of cloned variable groups
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IList<VariableGroup> CloneVariableGroups(IList<VariableGroup> source)
        {
            var clonedVariableGroups = new List<VariableGroup>();
            if (source == null)
            {
                return clonedVariableGroups;
            }

            foreach (var group in source)
            {
                if (group != null)
                {
                    clonedVariableGroups.Add(group.Clone());
                }
            }

            return clonedVariableGroups;
        }

        /// <summary>
        /// Replace secret values in group variables with null
        /// </summary>
        /// <param name="variableGroups">Variable groups to be cleared for secret variables</param>
        /// <returns>List of cleared variable groups</returns>
        public static IList<VariableGroup> ClearSecrets(IList<VariableGroup> variableGroups)
        {
            var groups = new List<VariableGroup>();

            if (variableGroups == null)
            {
                return groups;
            }

            foreach (var group in variableGroups)
            {
                if (group != null)
                {
                    var clearedGroup = group.Clone();

                    // Replacing secret variable's value with null
                    foreach (var variable in clearedGroup.Variables)
                    {
                        if (variable.Value != null && variable.Value.IsSecret)
                        {
                            variable.Value.Value = null;
                        }
                    }

                    groups.Add(clearedGroup);
                }
            }

            return groups;
        }

        /// <summary>
        /// Replace all secrets in variables with null
        /// </summary>
        /// <param name="variables">Variable set</param>
        /// <returns>Dictionary of variables</returns>
        public static IDictionary<String, VariableValue> ClearSecrets(IDictionary<String, VariableValue> variables)
        {
            var dictionary = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);

            if (variables == null)
            {
                return dictionary;
            }

            foreach (var kvp in variables)
            {
                if (kvp.Value != null)
                {
                    var clonedValue = kvp.Value.Clone();

                    if (kvp.Value.IsSecret)
                    {
                        clonedValue.Value = null;
                    }

                    dictionary[kvp.Key] = clonedValue;
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Check if any variable group has variable with secret value
        /// </summary>
        /// <param name="variableGroups">Variable groups to check if contains any secret variable with value.</param>
        /// <returns>Result</returns>
        public static bool HasSecretWithValue(IList<VariableGroup> variableGroups)
        {
            if (variableGroups == null || variableGroups.Count == 0)
            {
                return false;
            }

            foreach (var group in variableGroups)
            {
                if (group != null && HasSecretWithValue(group.Variables))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if Variables has any secret value
        /// </summary>
        /// <param name="variables">Variable set to check for any secret value</param>
        /// <returns></returns>
        public static bool HasSecretWithValue(IDictionary<String, VariableValue> variables)
        {
            if (variables == null || variables.Count == 0)
            {
                return false;
            }

            return variables.Any(s => s.Value != null &&
                                 s.Value.IsSecret &&
                                 !String.IsNullOrEmpty(s.Value.Value));
        }

        /// <summary>
        /// Check if any secret variable exists in variable group
        /// </summary>
        /// <param name="variableGroups">Variable groups to check if contains any secret variable</param>
        /// <returns>Result</returns>
        public static bool HasSecret(IList<VariableGroup> variableGroups)
        {
            if (variableGroups == null || variableGroups.Count == 0)
            {
                return false;
            }

            foreach (var group in variableGroups)
            {
                if (group != null && HasSecret(group.Variables))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if variable set contains any secret variable
        /// </summary>
        /// <param name="variables">Variable set to be checked for secret variable</param>
        /// <returns></returns>
        public static bool HasSecret(IDictionary<String, VariableValue> variables)
        {
            if (variables != null)
            {
                return variables.Any(v => v.Value != null && v.Value.IsSecret);
            }

            return false;
        }

        /// <summary>
        /// Copies secrets from source variable groups to target variable groups
        /// </summary>
        /// <param name="sourceGroups">Source variable groups</param>
        /// <param name="targetGroups">Target variable groups</param>
        /// <returns></returns>
        public static void FillSecrets(
            IList<VariableGroup> sourceGroups, 
            IList<VariableGroup> targetGroups)
        {
            if (sourceGroups == null || sourceGroups.Count == 0)
            {
                return;
            }

            if (targetGroups == null)
            {
                throw new ArgumentNullException("targetGroups");
            }

            foreach (var sourceGroup in sourceGroups)
            {
                var targetGroup = targetGroups.FirstOrDefault(group => group.Id == sourceGroup.Id);

                if (targetGroup != null)
                {
                    if (sourceGroup.Variables == null || sourceGroup.Variables.Count == 0)
                    {
                        // nothing to fill
                        continue;
                    }

                    if (targetGroup.Variables == null)
                    {
                        throw new ArgumentNullException(nameof(targetGroup.Variables));
                    }

                    foreach (var variable in sourceGroup.Variables.Where(x => x.Value.IsSecret))
                    {
                        targetGroup.Variables[variable.Key] = variable.Value.Clone();
                    }
                }
            }
        }

        private static Type keyVaultVariableType = typeof(AzureKeyVaultVariableValue);
    }
}