using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
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
            RepositoryResource repository,
            int buildDirectory,
            string hashKey)
        {
            var repoFullName = repository.Properties.Get<string>(RepositoryPropertyNames.Name);
            ArgUtil.NotNullOrEmpty(repoFullName, nameof(repoFullName));

            var repoName = repoFullName.Substring(repoFullName.LastIndexOf('/') + 1);
            ArgUtil.NotNullOrEmpty(repoName, nameof(repoName));
            // Set the directories.
            BuildDirectory = buildDirectory.ToString(CultureInfo.InvariantCulture);
            ArtifactsDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.ArtifactsDirectory);
            SourcesDirectory = Path.Combine(BuildDirectory, repoName);
            // TestResultsDirectory = Path.Combine(BuildDirectory, Constants.Build.Path.TestResultsDirectory);

            // Set the other properties.
            CollectionId = executionContext.Variables.System_CollectionId;
            DefinitionId = executionContext.Variables.System_DefinitionId;
            HashKey = hashKey;
            RepositoryUrl = repository.Url.AbsoluteUri;
            RepositoryType = repository.Type;
            System = BuildSystem;
            UpdateJobRunProperties(executionContext);
        }

        protected static readonly string BuildSystem = "build";

        public string CollectionId { get; set; }

        public string DefinitionId { get; set; }

        public string HashKey { get; set; }

        public string RepositoryUrl { get; set; }

        public string System { get; set; }

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

        public string RepositoryType { get; set; }

        [JsonIgnore]
        public DateTimeOffset? LastMaintenanceAttemptedOn { get; set; }

        [JsonProperty("lastMaintenanceAttemptedOn")]
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public string LastMaintenanceAttemptedOnString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", LastMaintenanceAttemptedOn);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    LastMaintenanceAttemptedOn = null;
                    return;
                }

                LastMaintenanceAttemptedOn = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [JsonIgnore]
        public DateTimeOffset? LastMaintenanceCompletedOn { get; set; }

        [JsonProperty("lastMaintenanceCompletedOn")]
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public string LastMaintenanceCompletedOnString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", LastMaintenanceCompletedOn);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    LastMaintenanceCompletedOn = null;
                    return;
                }

                LastMaintenanceCompletedOn = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [JsonProperty("build_sourcesdirectory")]
        public string SourcesDirectory { get; set; }

        public void UpdateJobRunProperties(IExecutionContext executionContext)
        {
            CollectionUrl = executionContext.Variables.System_TFCollectionUrl;
            DefinitionName = executionContext.Variables.Build_DefinitionName;
            LastRunOn = DateTimeOffset.Now;
        }
    }
}