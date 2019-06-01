using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class AgentMigrationMessage
    {
        public static readonly String MessageType = "AgentMigration";

        [JsonConstructor]
        internal AgentMigrationMessage()
        {
        }

        public AgentMigrationMessage(String accessToken)
        {
            this.AccessToken = accessToken;
        }

        [DataMember]
        public String AccessToken
        {
            get;
            private set;
        }
    }
}