using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [Obsolete("No longer used.")]
    [DataContract]
    public class BuildDestroyedEvent : RealtimeBuildEvent
    {
        public BuildDestroyedEvent(Build destroyedBuild)
            : base(destroyedBuild.Id)
        {
            this.Build = destroyedBuild;
        }

        [DataMember(IsRequired = true)]
        public Build Build
        {
            get;
            private set;
        }
    }
}
