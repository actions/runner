using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.ClientNotification
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class NotificationMessage
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public IDictionary<string, object> Attributes { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// 
        /// </summary>
        ProfileUpdate = 1,

        /// <summary>
        /// 
        /// </summary>
        ProfileAttributeUpdate = 2,

        /// <summary>
        /// 
        /// </summary>
        ProfileCoreAttributeUpdate = 3
    }
}
