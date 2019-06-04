using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ServiceEndpointReference : ResourceReference
    {
        public ServiceEndpointReference()
        {
        }

        private ServiceEndpointReference(ServiceEndpointReference referenceToCopy)
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

        public ServiceEndpointReference Clone()
        {
            return new ServiceEndpointReference(this);
        }

        public override String ToString()
        {
            return base.ToString() ?? this.Id.ToString("D");
        }
    }
}
