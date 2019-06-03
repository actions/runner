using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class ServiceEndpointExecutionRecordsInput
    {
        public ServiceEndpointExecutionRecordsInput()
        {
        }

        public ServiceEndpointExecutionRecordsInput(IList<Guid> endpointIds, ServiceEndpointExecutionData data)
        {
            this.EndpointIds = endpointIds;
            this.Data = data;
        }

        private ServiceEndpointExecutionRecordsInput(ServiceEndpointExecutionRecordsInput executionRecordsInputToBeCloned)
        {
            if (executionRecordsInputToBeCloned.EndpointIds != null)
            {
                this.EndpointIds = new List<Guid>(executionRecordsInputToBeCloned.EndpointIds);
            }

            if (executionRecordsInputToBeCloned.Data != null)
            {
                this.Data = executionRecordsInputToBeCloned.Data.Clone();
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<Guid> EndpointIds
        {
            get
            {
                return m_EndpointIds ?? (m_EndpointIds = new List<Guid>());
            }

            internal set
            {
                m_EndpointIds = value;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public ServiceEndpointExecutionData Data
        {
            get;
            internal set;
        }

        public ServiceEndpointExecutionRecordsInput Clone()
        {
            return new ServiceEndpointExecutionRecordsInput(this);
        }

        private IList<Guid> m_EndpointIds;
    }
}
