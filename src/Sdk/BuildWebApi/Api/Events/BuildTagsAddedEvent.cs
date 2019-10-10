using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [ServiceEventObject]
    public class BuildTagsAddedEvent : BuildUpdatedEvent
    {
        public BuildTagsAddedEvent(Build build, List<String> allTags, List<String> newTags)
            : base(build)
        {
            this.AllTags = allTags;
            this.NewTags = newTags;
        }

        [DataMember(IsRequired = true)]
        public List<String> AllTags
        {
            get;
            private set;
        }

        [DataMember(IsRequired = true)]
        public List<String> NewTags
        {
            get;
            private set;
        }
    }
}
