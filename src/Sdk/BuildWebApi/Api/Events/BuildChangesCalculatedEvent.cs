using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [Obsolete("Use BuildEvent instead.")]
    public class BuildChangesCalculatedEvent : BuildUpdatedEvent
    {
        public BuildChangesCalculatedEvent(
            Build build,
            List<Change> changes)
            : base(build)
        {
            this.Changes = changes;
        }

        [DataMember(IsRequired = true)]
        public List<Change> Changes
        {
            get;
            private set;
        }
    }
}
