using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseTrackingConfig
    {
        [JsonProperty("releaseDirectory")]
        public string ReleaseDirectory { get; set; }

        public const string FileFormatVersionJsonProperty = "fileFormatVersion";

        // The parameterless constructor is required for deserialization.
        public ReleaseTrackingConfig()
        {
        }

        [JsonProperty(FileFormatVersionJsonProperty)]
        public int FileFormatVersion
        {
            get
            {
                return 1;
            }

            set
            {
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

        public void UpdateJobRunProperties()
        {
            LastRunOn = DateTimeOffset.Now;
        }
    }
}