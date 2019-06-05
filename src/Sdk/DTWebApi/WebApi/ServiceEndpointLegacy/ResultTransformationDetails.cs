using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ResultTransformationDetails
    {
        public ResultTransformationDetails()
        {
        }

        private ResultTransformationDetails(ResultTransformationDetails resultTransformationDetailsToClone)
        {
            this.ResultTemplate = resultTransformationDetailsToClone.ResultTemplate;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ResultTemplate
        {
            get;
            set;
        }

        public ResultTransformationDetails Clone()
        {
            return new ResultTransformationDetails(this);
        }
    }
}
