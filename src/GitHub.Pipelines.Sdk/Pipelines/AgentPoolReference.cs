using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class AgentPoolReference : ResourceReference
    {
        public AgentPoolReference()
        {
        }

        private AgentPoolReference(AgentPoolReference referenceToCopy)
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

        public AgentPoolReference Clone()
        {
            return new AgentPoolReference(this);
        }

        public override String ToString()
        {
            return base.ToString() ?? this.Id.ToString();
        }
    }
}
