using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    public sealed class TrackingConfig
    {
        public const string FileFormatVersionJsonProperty = "fileFormatVersion";

        // The parameterless constructor is required for deserialization.
        public TrackingConfig()
        {
        }

        public TrackingConfig(
            IExecutionContext executionContext,
            int pipelineDirectory,
            string hashKey)
        {
            var repoFullName = executionContext.GetGitHubContext("repository");
            ArgUtil.NotNullOrEmpty(repoFullName, nameof(repoFullName));

            var repoName = repoFullName.Substring(repoFullName.LastIndexOf('/') + 1);
            ArgUtil.NotNullOrEmpty(repoName, nameof(repoName));
            // Set the directories.
            PipelineDirectory = pipelineDirectory.ToString(CultureInfo.InvariantCulture);
            ArtifactsDirectory = Path.Combine(PipelineDirectory, Constants.Build.Path.ArtifactsDirectory);
            SourcesDirectory = Path.Combine(PipelineDirectory, repoName);

            // Set the other properties.
            CollectionId = executionContext.Variables.System_CollectionId;
            DefinitionId = executionContext.Variables.System_DefinitionId;
            HashKey = hashKey;
            RepositoryUrl = $"https://github.com/{repoFullName}";
            UpdateJobRunProperties(executionContext);
        }

        public string CollectionId { get; set; }

        public string DefinitionId { get; set; }

        public string HashKey { get; set; }

        public string RepositoryUrl { get; set; }

        [JsonProperty("runner_artifactdirectory")]
        public string ArtifactsDirectory { get; set; }

        [JsonProperty("runner_pipelinedirectory")]
        public string PipelineDirectory { get; set; }

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

        [JsonProperty("runner_sourcesdirectory")]
        public string SourcesDirectory { get; set; }

        public void UpdateJobRunProperties(IExecutionContext executionContext)
        {
            CollectionUrl = executionContext.Variables.System_TFCollectionUrl;
            DefinitionName = executionContext.Variables.Build_DefinitionName;
            LastRunOn = DateTimeOffset.Now;
        }
    }
}
