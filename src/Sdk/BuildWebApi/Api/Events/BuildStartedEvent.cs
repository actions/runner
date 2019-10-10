using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [Obsolete("No longer used.")]
    public class BuildStartedEvent : BuildUpdatedEvent
    {
        public BuildStartedEvent(Build build)
            : base(build)
        {
        }
    }
}
