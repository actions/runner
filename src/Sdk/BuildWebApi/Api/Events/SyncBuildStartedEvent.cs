using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [ServiceEventObject]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("No longer used.")]
    public class SyncBuildStartedEvent : BuildUpdatedEvent
    {
        internal SyncBuildStartedEvent(Build build)
            : base(build)
        {
        }
    }
}
