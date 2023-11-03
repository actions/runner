using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class AgentRefreshMessage
    {
        public static readonly String MessageType = "AgentRefresh";

        [JsonConstructor]
        internal AgentRefreshMessage()
        {
        }

        public AgentRefreshMessage(
            ulong agentId,
            String targetVersion,
            TimeSpan? timeout = null)
        {
            this.AgentId = agentId;
            this.Timeout = timeout ?? TimeSpan.FromMinutes(60);
            this.TargetVersion = targetVersion;
        }

        [DataMember]
        public ulong AgentId
        {
            get;
            private set;
        }

        [DataMember]
        public TimeSpan Timeout
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
        public PackageMetadata PackageMetadata
        {
            get;
            private set;
        }
    }
}
