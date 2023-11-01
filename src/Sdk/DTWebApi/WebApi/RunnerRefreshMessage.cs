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

        [JsonConstructor]
        internal RunnerRefreshMessage()
        {
        }
        public RunnerRefreshMessage(
            ulong runnerId,
            String targetVersion,
            int? timeoutInSeconds = null)
        {
            this.RunnerId = runnerId;
            this.TimeoutInSeconds = timeoutInSeconds ?? TimeSpan.FromMinutes(60).Seconds;
            this.TargetVersion = targetVersion;
        }

        [DataMember]
        public ulong RunnerId
        {
            get;
            private set;
        }

        [DataMember]
        public int TimeoutInSeconds
        {
            get;
            private set;
        }

        [DataMember]
        public String TargetVersion
        {
            get;
            private set;
        }

        [DataMember]
        public BrokerPackageMetadata Package
        {
            get;
            set;
        }

         public PackageMetadata GetPackageMetadata()
            {
                return new PackageMetadata()
                {
                    DownloadUrl = this.Package?.DownloadUrl,
                    HashValue = this.Package?.HashValue,
                    Platform = this.Package?.Platform,
                    Version = new PackageVersion(this.TargetVersion)
                };
            }
    }
}
