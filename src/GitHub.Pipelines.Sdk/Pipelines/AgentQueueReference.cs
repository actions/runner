using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class AgentQueueReference : ResourceReference
    {
        public AgentQueueReference()
        {
        }

        private AgentQueueReference(AgentQueueReference referenceToCopy)
            : base(referenceToCopy)
        {
            this.Id = referenceToCopy.Id;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            set;
        }

        public AgentQueueReference Clone()
        {
            return new AgentQueueReference(this);
        }

        public override String ToString()
        {
            return base.ToString() ?? this.Id.ToString();
        }
    }
}
