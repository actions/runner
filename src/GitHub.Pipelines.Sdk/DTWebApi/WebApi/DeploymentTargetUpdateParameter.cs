using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment target update parameter.
    /// </summary>
    [DataContract]
    public class DeploymentTargetUpdateParameter
    {
        /// <summary>
        /// Identifier of the deployment target.
        /// </summary>
        [DataMember]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// Tags of the deployment target..
        /// </summary>
        public IList<String> Tags
        {
            get
            {
                return m_tags;
            }
            set
            {
                m_tags = value;
            }
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Tags")]
        private IList<String> m_tags;
    }
}
