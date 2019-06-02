using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SecureFileReference : ResourceReference
    {
        public SecureFileReference()
        {
        }

        private SecureFileReference(SecureFileReference referenceToCopy)
            : base(referenceToCopy)
        {
            this.Id = referenceToCopy.Id;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }

        public SecureFileReference Clone()
        {
            return new SecureFileReference(this);
        }

        public override String ToString()
        {
            return base.ToString() ?? this.Id.ToString("D");
        }
    }
}
