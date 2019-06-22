using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [Obsolete("No longer used")]
    public class BuildDefinitionChangedEvent
    {
        public BuildDefinitionChangedEvent()
        {
        }

        public BuildDefinitionChangedEvent(AuditAction changeType, BuildDefinition definition)
        {
            Definition = definition;
            ChangeType = changeType;
        }

        [DataMember]
        public BuildDefinition Definition
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
