using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    public class BuildDeletedEvent : RealtimeBuildEvent
    {
        public BuildDeletedEvent(Build deletedBuild)
            : base(deletedBuild.Id)
        {
            this.Build = deletedBuild;
        }

        [DataMember(IsRequired = true)]
        public Build Build
        {
            get;
            private set;
        }
    }
}
