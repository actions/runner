using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment target.
    /// </summary>
    [DataContract]
    public class DeploymentMachine : ICloneable
    {
        public DeploymentMachine()
        {
        }

        private DeploymentMachine(DeploymentMachine machineToBeCloned)
        {
            this.Id = machineToBeCloned.Id;
            this.Tags = (Tags == null) ? null : new List<String>(machineToBeCloned.Tags);
            this.Agent = machineToBeCloned.Agent?.Clone();
        }

        /// <summary>
        /// Deployment target Identifier.
        /// </summary>
        [DataMember]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// Tags of the deployment target.
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

        /// <summary>
        /// Deployment agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgent Agent
        {
            get;
            set;
        }

        public PropertiesCollection Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new PropertiesCollection();
                }

                return m_properties;
            }
            internal set
            {
                m_properties = value;
            }
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public DeploymentMachine Clone()
        {
            return new DeploymentMachine(this);
        }

        /// <summary>
        /// Tags of the deployment target.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Tags")]
        private IList<String> m_tags;

        /// <summary>
        /// Properties of the deployment target.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;
    }
}
