using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class ServiceEndpointExecutionRecord
    {
        public ServiceEndpointExecutionRecord()
        {
        }

        public ServiceEndpointExecutionRecord(Guid endpointId, ServiceEndpointExecutionData data)
        {
            this.EndpointId = endpointId;
            this.Data = data;
        }

        private ServiceEndpointExecutionRecord(ServiceEndpointExecutionRecord executionRecordToBeCloned)
        {
            this.EndpointId = executionRecordToBeCloned.EndpointId;
            
            if (executionRecordToBeCloned.Data != null)
            {
                this.Data = executionRecordToBeCloned.Data.Clone();
            }
        }

        /// <summary>
        /// Gets the Id of service endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid EndpointId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the execution data of service endpoint execution.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ServiceEndpointExecutionData Data
        {
            get;
            internal set;
        }

        public ServiceEndpointExecutionRecord Clone()
        {
            return new ServiceEndpointExecutionRecord(this);
        }
    }
}
