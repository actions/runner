using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class RunnerRefreshMessage
    {
        public static readonly String MessageType = "RunnerRefresh";

        [JsonConstructor]
        internal RunnerRefreshMessage()
        {
        }

        [DataMember(Name = "target_version")]
        public String TargetVersion
        {
            get;
            set;
        }

        [DataMember(Name = "download_url")]
        public string DownloadUrl
        {
            get;
            set;
        }

        [DataMember(Name = "sha256_checksum")]
        public string SHA256Checksum
        {
            get;
            set;
        }

        [DataMember(Name = "os")]
        public string OS
        {
            get;
            set;
        }
    }
}
