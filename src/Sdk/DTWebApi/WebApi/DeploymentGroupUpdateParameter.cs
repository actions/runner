using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment group update parameter.
    /// </summary>
    [DataContract]
    public class DeploymentGroupUpdateParameter
    {
        /// <summary>
        /// Name of the deployment group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Description of the deployment group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }
    }
}
