using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TopLevelTrackingConfig
    {
        [JsonIgnore]
        public DateTimeOffset? LastBuildDirectoryCreatedOn { get; set; }

        [JsonProperty("lastBuildFolderCreatedOn")]
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public string LastBuildDirectoryCreatedOnString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", this.LastBuildDirectoryCreatedOn);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.LastBuildDirectoryCreatedOn = null;
                    return;
                }

                this.LastBuildDirectoryCreatedOn = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [JsonProperty("lastBuildFolderNumber")]
        public int LastBuildDirectoryNumber { get; set; }
    }
}