namespace GitHub.DistributedTask.WebApi
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ServiceEndpointRequest
    {
        public ServiceEndpointRequest()
        {
        }

        private ServiceEndpointRequest(ServiceEndpointRequest endpointRequestToClone)
        {
            if (endpointRequestToClone.ServiceEndpointDetails != null)
            {
                this.ServiceEndpointDetails = endpointRequestToClone.ServiceEndpointDetails.Clone();
            }

            if (endpointRequestToClone.DataSourceDetails != null)
            {
                this.DataSourceDetails = endpointRequestToClone.DataSourceDetails.Clone();
            }

            if (endpointRequestToClone.ResultTransformationDetails != null)
            {
                this.ResultTransformationDetails = endpointRequestToClone.ResultTransformationDetails.Clone();
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public ServiceEndpointDetails ServiceEndpointDetails
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DataSourceDetails DataSourceDetails
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ResultTransformationDetails ResultTransformationDetails
        {
            get;
            set;
        }

        public ServiceEndpointRequest Clone()
        {
            return new ServiceEndpointRequest(this);
        }
    }
}
