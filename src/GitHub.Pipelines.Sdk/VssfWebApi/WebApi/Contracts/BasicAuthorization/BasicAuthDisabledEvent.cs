using System;
using System.Runtime.Serialization;
using GitHub.Services.Notifications;

namespace GitHub.Services.BasicAuthorization
{
    [DataContract]
    [NotificationEventBindings(EventSerializerType.Json, "ms.vss-sps-notifications.basic-auth-disabled-event")]
    public class BasicAuthDisabledEvent
    {
        public BasicAuthDisabledEvent()
        {
        }
    }
}
