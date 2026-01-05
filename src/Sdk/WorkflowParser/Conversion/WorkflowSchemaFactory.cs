using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Schema;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Loads the schema for workflows
    /// </summary>
    internal static class WorkflowSchemaFactory
    {
        /// <summary>
        /// Loads the template schema for the specified features.
        /// </summary>
        internal static TemplateSchema GetSchema(WorkflowFeatures features)
        {
            if (features == null)
            {
                throw new System.ArgumentNullException(nameof(features));
            }

            // Find resource names corresponding to enabled features
            var resourceNames = WorkflowFeatures.Names
                .Where(x => features.GetFeature(x))         // Enabled features only
                .Select(x => string.Concat(c_resourcePrefix, "-", x, c_resourceSuffix)) // To resource name
                .Where(x => s_resourceNames.Contains(x))    // Resource must exist
                .ToList();

            // More than one resource found?
            if (resourceNames.Count > 1)
            {
                throw new NotSupportedException("Failed to load workflow schema. Only one feature flag with schema changes can be enabled at a time.");
            }

            var resourceName = resourceNames.FirstOrDefault() ?? c_defaultResourceName;
            return s_schemas.GetOrAdd(
                resourceName,
                (resourceName) =>
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var json = default(String);
                    using (var stream = assembly.GetManifestResourceStream(resourceName)!)
                    using (var streamReader = new StreamReader(stream))
                    {
                        json = streamReader.ReadToEnd();
                    }

                    var objectReader = new JsonObjectReader(null, json);
                    return TemplateSchema.Load(objectReader);
                });
        }

        private const string c_resourcePrefix = "GitHub.Actions.WorkflowParser.workflow-v1.0";
        private const string c_resourceSuffix = ".json";
        private const string c_defaultResourceName = c_resourcePrefix + c_resourceSuffix;
        private static readonly HashSet<string> s_resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames().ToHashSet(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, TemplateSchema> s_schemas = new(StringComparer.Ordinal);
    }
}
