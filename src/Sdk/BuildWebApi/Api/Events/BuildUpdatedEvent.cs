using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    public class BuildUpdatedEvent : RealtimeBuildEvent
    {
        public BuildUpdatedEvent(Build build)
            : base(build.Id)
        {
            this.Build = build;
        }

        [DataMember(IsRequired = true)]
        public Build Build
        {
            get;
            private set;
        }
    }
}
