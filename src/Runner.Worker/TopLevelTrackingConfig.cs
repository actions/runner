using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace GitHub.Runner.Worker
{
    public sealed class TopLevelTrackingConfig
    {
        [JsonIgnore]
        public DateTimeOffset? LastPipelineDirectoryCreatedOn { get; set; }

        [JsonProperty("lastPipelineFolderCreatedOn")]
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public string LastPipelineDirectoryCreatedOnString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", LastPipelineDirectoryCreatedOn);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    LastPipelineDirectoryCreatedOn = null;
                    return;
                }

                LastPipelineDirectoryCreatedOn = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [JsonProperty("lastPipelineFolderNumber")]
        public int LastPipelineDirectoryNumber { get; set; }
    }
}
