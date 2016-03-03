using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TrackingConfig : TrackingConfigBase
    {
        public const string FileFormatVersionJsonProperty = "fileFormatVersion";

        // The parameterless constructor is required for deserialization.
        public TrackingConfig()
        {
        }

        public TrackingConfig(
            IExecutionContext executionContext,
            LegacyTrackingConfig copy,
            string sourcesDirectoryNameOnly,
            bool useNewArtifactsDirectoryName = false)
        {
            // Set the directories.
            BuildDirectory = Path.GetFileName(copy.BuildDirectory); // Just take the portion after _work folder.
            String artifactsDirectoryNameOnly =
                useNewArtifactsDirectoryName ? Constants.Build.Path.ArtifactsDirectory : Constants.Build.Path.LegacyArtifactsDirectory;
            ArtifactsDirectory = Path.Combine(BuildDirectory, artifactsDirectoryNameOnly);
            SourcesDirectory = Path.Combine(BuildDirectory, sourcesDirectoryNameOnly);
            TestResultsDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.TestResultsDirectory);

            // Set the other properties.
            CollectionId = copy.CollectionId;
            DefinitionId = copy.DefinitionId;
            HashKey = copy.HashKey;
            RepositoryUrl = copy.RepositoryUrl;
            System = copy.System;
            SetCollectionName(executionContext);
        }

        public TrackingConfig(IExecutionContext executionContext, ServiceEndpoint endpoint, int buildDirectory, string hashKey)
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
            RepositoryUrl = endpoint.Url.AbsoluteUri;
            System = BuildSystem;
            SetCollectionName(executionContext);
            UpdateJobRunProperties(executionContext);
        }

        [JsonProperty("build_artifactstagingdirectory")]
        public string ArtifactsDirectory { get; set; }

        [JsonProperty("agent_builddirectory")]
        public string BuildDirectory { get; set; }

        public string CollectionName { get; set; }

        public string DefinitionName { get; set; }

        [JsonProperty(FileFormatVersionJsonProperty)]
        public int FileFormatVersion
        {
            get { return 2; }

            set
            {
                if (value != 2)
                {
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
            // Set the collection name.
            DefinitionName = executionContext.Variables.Build_DefinitionName;
            LastRunOn = DateTimeOffset.Now;
        }

        private void SetCollectionName(IExecutionContext executionContext)
        {
            string collectionUrlString = executionContext.Variables.System_TFCollectionUrl;
            Uri collectionUrl;
            if (!Uri.TryCreate(collectionUrlString, UriKind.Absolute, out collectionUrl))
            {
                CollectionName = string.Empty;
            }
            else
            {
                string lastSegment = (collectionUrl.Segments.LastOrDefault() ?? String.Empty).TrimEnd('/');
                CollectionName = Uri.UnescapeDataString(lastSegment);
            }
        }
    }
}