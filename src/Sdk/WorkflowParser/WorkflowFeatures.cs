using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    /// <summary>
    /// Features flags (mostly short-lived)
    /// </summary>
    [DataContract]
    public class WorkflowFeatures
    {
        /// <summary>
        /// Gets or sets a value indicating whether users may specify permission "id-token".
        /// Used during parsing only.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IdToken { get; set; } // Remove with DistributedTask.AllowGenerateIdToken

        /// <summary>
        /// Gets or sets a value indicating whether users may specify permission "short-matrix-ids".
        /// Used during parsing and evaluation.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool ShortMatrixIds { get; set; } // Remove with DistributedTask.GenerateShortMatrixIds

        /// <summary>
        /// Gets or sets a value indicating whether users may use the "snapshot" keyword. 
        /// Used during parsing only.
        /// More information: https://github.com/github/hosted-runners/issues/186 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Snapshot { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether users may use the "models" permission. 
        /// Used during parsing only.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool AllowModelsPermission { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the expression function fromJson performs strict JSON parsing.
        /// Used during evaluation only.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool StrictJsonParsing { get; set; }

        /// <summary>
        /// Gets the default workflow features.
        /// </summary>
        public static WorkflowFeatures GetDefaults()
        {
            return new WorkflowFeatures
            {
                IdToken = true,             // Default to true since this is a long-lived feature flag
                ShortMatrixIds = true,      // Default to true since this is a long-lived feature flag
                Snapshot = false,           // Default to false since this feature is still in an experimental phase
                StrictJsonParsing = false,  // Default to false since this is temporary for telemetry purposes only
                AllowModelsPermission = false, // Default to false since we want this to be disabled for all non-production environments
            };
        }

        /// <summary>
        /// Gets the value of the feature flag
        /// </summary>
        public bool GetFeature(string name)
        {
            return (bool)s_properties[name].GetValue(this)!;
        }

        /// <summary>
        /// Sets the value of the feature flag
        /// </summary>
        public void SetFeature(string name, bool value)
        {
            s_properties[name].SetValue(this, value);
        }

        /// <summary>
        /// Reflection info for accessing the feature flags
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> s_properties =
            typeof(WorkflowFeatures).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.PropertyType == typeof(bool)) // Boolean properties only
            .ToDictionary(x => x.Name, StringComparer.Ordinal);

        /// <summary>
        /// Names of all feature flags
        /// </summary>
        public static readonly IReadOnlyList<string> Names = s_properties.Keys.Order().ToList().AsReadOnly();
    }
}
