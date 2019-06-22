using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [ServiceEventObject]
    public class BuildQueuedEvent : BuildUpdatedEvent
    {
        public BuildQueuedEvent(Build build)
            : base(build)
        {
        }
    }
}
