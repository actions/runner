using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [Obsolete("No longer used")]
    public class BuildDefinitionChangingEvent
    {
        public BuildDefinitionChangingEvent()
        {
        }

        public BuildDefinitionChangingEvent(AuditAction changeType, BuildDefinition originalDefinition, BuildDefinition newDefinition)
        {
            OriginalDefinition = originalDefinition;
            NewDefinition = newDefinition;
            ChangeType = changeType;
        }

        [DataMember]
        public BuildDefinition OriginalDefinition
        {
            get;
            set;
        }

        [DataMember]
        public BuildDefinition NewDefinition
        {
            get;
            set;
        }

        [DataMember]
        public AuditAction ChangeType
        {
            get;
            set;
        }
    }
}
