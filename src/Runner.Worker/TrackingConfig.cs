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
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    public sealed class RepositoryTrackingConfig
    {
        public string RepositoryPath { get; set; }

        [JsonIgnore]
        public DateTimeOffset? LastRunOn { get; set; }

        [JsonProperty("LastRunOn")]
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
    }

    public sealed class TrackingConfig
    {
        // The parameterless constructor is required for deserialization.
        public TrackingConfig()
        {
        }

        public TrackingConfig(IExecutionContext executionContext)
        {
            var repoFullName = executionContext.GetGitHubContext("repository");
            ArgUtil.NotNullOrEmpty(repoFullName, nameof(repoFullName));
            RepositoryName = repoFullName;

            var repoName = repoFullName.Substring(repoFullName.LastIndexOf('/') + 1);
            ArgUtil.NotNullOrEmpty(repoName, nameof(repoName));

            // Set the directories.
            PipelineDirectory = repoName.ToString(CultureInfo.InvariantCulture);
            WorkspaceDirectory = Path.Combine(PipelineDirectory, repoName);

            Repositories[repoFullName] = new RepositoryTrackingConfig()
            {
                LastRunOn = DateTimeOffset.Now,
                RepositoryPath = WorkspaceDirectory
            };

            // Set the other properties.
            LastRunOn = DateTimeOffset.Now;
        }

        private Dictionary<string, RepositoryTrackingConfig> _repositories;

        public string RepositoryName { get; set; }

        public string PipelineDirectory { get; set; }

        public string WorkspaceDirectory { get; set; }

        public Dictionary<string, RepositoryTrackingConfig> Repositories
        {
            get
            {
                if (_repositories == null)
                {
                    _repositories = new Dictionary<string, RepositoryTrackingConfig>(StringComparer.OrdinalIgnoreCase);
                }

                return _repositories;
            }
        }

        [JsonIgnore]
        public DateTimeOffset? LastRunOn { get; set; }

        [JsonProperty("LastRunOn")]
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
    }
}
