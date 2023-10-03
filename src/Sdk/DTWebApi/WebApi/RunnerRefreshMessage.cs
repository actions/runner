using Newtonsoft.Json;
using System;
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
    }
}
