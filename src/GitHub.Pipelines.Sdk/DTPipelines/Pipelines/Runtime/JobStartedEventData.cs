using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime
{
    [DataContract]
    public sealed class JobStartedEventData
    {
        [DataMember(EmitDefaultValue = false)]
        public PhaseTargetType JobType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid JobId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Object Data
        {
            get;
            set;
        }
    }
}
