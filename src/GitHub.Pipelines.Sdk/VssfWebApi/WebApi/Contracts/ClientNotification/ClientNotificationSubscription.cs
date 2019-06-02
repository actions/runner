using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.ClientNotification
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class ClientNotificationSubscription
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Guid IdentityId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ServiceUri { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string MessageReceiver { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int PrefetchCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int MaxRetriesCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool UseAmqpTransportType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int ReceiveOperationTimeoutInMinutes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int ServiceBusRetryDelayMilliSecounds { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int ServiceBusLongRetryDelayMilliSecounds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ClientSignatureToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ClientSignatureTokenName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public DateTimeOffset ClientSignatureTokenExpiration { get; set; }
    }
}