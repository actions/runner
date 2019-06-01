using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Notifications;

namespace Microsoft.VisualStudio.Services.BasicAuthorization
{
    [DataContract]
    [NotificationEventBindings(EventSerializerType.Json, "ms.vss-sps-notifications.basic-auth-password-changed-event")]
    public class BasicAuthPasswordChangedEvent
    {
        public BasicAuthPasswordChangedEvent()
        {
        }
    }
}
