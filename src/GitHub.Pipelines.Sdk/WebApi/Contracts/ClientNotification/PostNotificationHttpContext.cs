using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.ClientNotification
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class PostNotificationHttpContext
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid IdentityId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public NotificationMessage Message { get; set; }
    }
}
