using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Message that tells the runner to redirect itself to BrokerListener for messages.
    /// (Note that we use a special Message instead of a simple 302. This is because 
    /// the runner will need to apply the runner's token to the request, and it is
    /// a security best practice to *not* blindly add sensitive data to redirects
    /// 302s.)
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
        /// The base url for the broker listener
        /// </summary>
        [DataMember]
        public Uri BrokerBaseUrl
        {
            get;
            internal set;
        }
    }
}
