using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class TrackingConfig2 : TrackingConfigBase2
    {
        public const string FileFormatVersionJsonProperty = "fileFormatVersion";

        // The parameterless constructor is required for deserialization.
        public TrackingConfig2()
        {
        }

        public TrackingConfig2(
            IExecutionContext executionContext,
            LegacyTrackingConfig2 copy,
            string sourcesDirectoryNameOnly,
            bool useNewArtifactsDirectoryName = false)
        {
            // Set the directories.
            BuildDirectory = Path.GetFileName(copy.BuildDirectory); // Just take the portion after _work folder.
            string artifactsDirectoryNameOnly =
                useNewArtifactsDirectoryName ? Constants.Build.Path.ArtifactsDirectory : Constants.Build.Path.LegacyArtifactsDirectory;
            ArtifactsDirectory = Path.Combine(BuildDirectory, artifactsDirectoryNameOnly);
            SourcesDirectory = Path.Combine(BuildDirectory, sourcesDirectoryNameOnly);
            TestResultsDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.TestResultsDirectory);

            // Set the other properties.
            CollectionId = copy.CollectionId;
            CollectionUrl = executionContext.Variables.System_TFCollectionUrl;
            DefinitionId = copy.DefinitionId;
            HashKey = copy.HashKey;
            RepositoryUrl = copy.RepositoryUrl;
            System = copy.System;
        }

        public TrackingConfig2(IExecutionContext executionContext, string repositoryUrl, int buildDirectory, string hashKey)
        {
            // Set the directories.
            BuildDirectory = buildDirectory.ToString(CultureInfo.InvariantCulture);
            ArtifactsDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.ArtifactsDirectory);
            SourcesDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.SourcesDirectory);
            TestResultsDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.TestResultsDirectory);

            // Set the other properties.
            CollectionId = executionContext.Variables.System_CollectionId;
            DefinitionId = executionContext.Variables.System_DefinitionId;
            HashKey = hashKey;
            RepositoryUrl = repositoryUrl;
            System = BuildSystem;
            UpdateJobRunProperties(executionContext);
        }

        [JsonProperty("build_artifactstagingdirectory")]
        public string ArtifactsDirectory { get; set; }

        [JsonProperty("agent_builddirectory")]
        public string BuildDirectory { get; set; }

        public string CollectionUrl { get; set; }

        public string DefinitionName { get; set; }

        [JsonProperty(FileFormatVersionJsonProperty)]
        public int FileFormatVersion
        {
            get
            {
                return 3;
            }

            set
            {
                // Version 3 changes:
                //   CollectionName was removed.
                //   CollectionUrl was added.
                switch (value)
                {
                    case 3:
                    case 2:
                        break;
                    default:
                        // Should never reach here.
                        throw new NotSupportedException();
                }
            }
        }

        [JsonIgnore]
        public DateTimeOffset? LastRunOn { get; set; }

        [JsonProperty("lastRunOn")]
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public string LastRunOnString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", LastRunOn);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    LastRunOn = null;
                    return;
                }

                LastRunOn = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [JsonProperty("build_sourcesdirectory")]
        public string SourcesDirectory { get; set; }

        [JsonProperty("common_testresultsdirectory")]
        public string TestResultsDirectory { get; set; }

        public void UpdateJobRunProperties(IExecutionContext executionContext)
        {
            CollectionUrl = executionContext.Variables.System_TFCollectionUrl;
            DefinitionName = executionContext.Variables.Build_DefinitionName;
            LastRunOn = DateTimeOffset.Now;
        }
    }
}