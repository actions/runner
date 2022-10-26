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
            Int32 agentId,
            String targetVersion,
            int? timeout = null)
        {
            this.AgentId = agentId;
            this.Timeout = timeout ?? TimeSpan.FromMinutes(60).Milliseconds;
            this.TargetVersion = targetVersion;
        }

        [DataMember]
        public Int32 AgentId
        {
            get;
            private set;
        }

        [DataMember]
        public int Timeout
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
