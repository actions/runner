using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class DeploymentMachineGroup : DeploymentMachineGroupReference
    {
        [DataMember]
        public Int32 Size
        {
            get;
            internal set;
        }

        public IList<DeploymentMachine> Machines
        {
            get
            {
                if (m_machines == null)
                {
                    m_machines = new List<DeploymentMachine>();
                }

                return m_machines;
            }

            internal set
            {
                m_machines = value;
            }
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Machines")]
        private IList<DeploymentMachine> m_machines;
    }
}
