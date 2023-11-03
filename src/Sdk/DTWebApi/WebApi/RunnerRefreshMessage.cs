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
        public RunnerRefreshMessage(
            ulong runnerId,
            String targetVersion)
        {
            this.RunnerId = runnerId;
            this.TimeoutInSeconds = timeoutInSeconds ?? TimeSpan.FromMinutes(60).Seconds;
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
