using System;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class LegacyTrackingConfig2 : TrackingConfigBase2
    {
        // The property name in the config file is misleading. The value really represents
        // the build folder - i.e. the folder that contains the source folder.
        [JsonProperty("sourceFolder")]
        public string BuildDirectory { get; set; }

        public static LegacyTrackingConfig2 TryParse(string content)
        {
            // Fix the content to be valid JSON syntax. The file version 1 files
            // were written as:
            //     { 
            //         "system"" : "[...]", 
            //         "collectionId"" = "[...]", 
            //         "definitionId"" = "[...]", 
            //         "repositoryUrl"" = "[...]", 
            //         "sourceFolder" = "[...]",
            //         "hashKey" = "[...]"
            //     }
            //
            // Furthermore, the values were not JSON-escaped.
            content =
                content
                // Escape special characters.
                .Replace(@"\", @"\\")
                // Change "=" to ":".
                .Replace(@"""collectionId"" = ", @"""collectionId"": ")
                .Replace(@"""definitionId"" = ", @"""definitionId"": ")
                .Replace(@"""repositoryUrl"" = ", @"""repositoryUrl"": ")
                .Replace(@"""sourceFolder"" = ", @"""sourceFolder"": ")
                .Replace(@"""hashKey"" = ", @"""hashKey"": ");
            LegacyTrackingConfig2 config = null;
            try
            {
                config = JsonConvert.DeserializeObject<LegacyTrackingConfig2>(content);
            }
            catch (Exception)
            {
            }

            if (config != null
                && !string.IsNullOrEmpty(config.BuildDirectory)
                && !string.IsNullOrEmpty(config.CollectionId)
                && !string.IsNullOrEmpty(config.DefinitionId)
                && !string.IsNullOrEmpty(config.HashKey)
                && !string.IsNullOrEmpty(config.RepositoryUrl)
                && !string.IsNullOrEmpty(config.System))
            {
                return config;
            }

            return null;
        }
    }
}