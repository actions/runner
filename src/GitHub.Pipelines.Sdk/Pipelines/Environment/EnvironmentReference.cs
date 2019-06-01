using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class EnvironmentReference : ResourceReference
    {
        public EnvironmentReference()
        {
        }

        private EnvironmentReference(EnvironmentReference referenceToCopy)
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

        public EnvironmentReference Clone()
        {
            return new EnvironmentReference(this);
        }

        public override String ToString()
        {
            return base.ToString() ?? this.Id.ToString();
        }
    }
}
