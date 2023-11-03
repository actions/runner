using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class BrokerPackageMetadata
    {
        [JsonConstructor]
        internal BrokerPackageMetadata()
        {
        }

        [DataMember(Name = "download_url")]
        public string DownloadUrl
        {
            get;
            set;
        }

        [DataMember(Name = "sha256_checksum")]
        public string HashValue
        {
            get;
            set;
        }

        [DataMember(Name = "os")]
        public string Platform
        {
            get;
            set;
        }
    }
}
