using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [Obsolete("Use BuildEvent instead.")]
    public class BuildArtifactAddedEvent : BuildUpdatedEvent
    {
        public BuildArtifactAddedEvent(
            Build build,
            BuildArtifact artifact)
            : base(build)
        {
            this.Artifact = artifact;
        }

        [DataMember(IsRequired = true)]
        public BuildArtifact Artifact
        {
            get;
            private set;
        }
    }
}
