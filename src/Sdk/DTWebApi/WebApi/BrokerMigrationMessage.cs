using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents a session for performing message exchanges from an agent.
    /// </summary>
    [DataContract]
    public class BrokerMigrationMessage
    {

        public static readonly string MessageType = "BrokerMigration";

        public BrokerMigrationMessage()
        {
        }

        public BrokerMigrationMessage(
            Uri brokerUrl)
        {
            this.BrokerBaseUrl = brokerUrl;
        }

        /// <summary>
        /// Gets the unique identifier for this session.
        /// </summary>
        [DataMember]
        public Uri BrokerBaseUrl
        {
            get;
            internal set;
        }
    }
}
