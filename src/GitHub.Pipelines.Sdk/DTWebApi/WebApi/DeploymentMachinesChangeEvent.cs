using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class DeploymentMachinesChangeEvent
    {
        public DeploymentMachinesChangeEvent(
            DeploymentGroupReference machineGroupReference, 
            IList<DeploymentMachineChangedData> machines)
        {
            ArgumentUtility.CheckForNull(machineGroupReference, "machineGroupReference");
            MachineGroupReference = machineGroupReference;
            m_machines = machines;
        }

        public IList<DeploymentMachineChangedData> Machines
        {
            get
            {
                if (m_machines == null)
                {
                    m_machines = new List<DeploymentMachineChangedData>();
                }

                return m_machines;
            }
        }

        [DataMember]
        public DeploymentGroupReference MachineGroupReference
        {
            get;
            private set;
        }

        [DataMember(Name = "Machines")]
        private IList<DeploymentMachineChangedData> m_machines;
    }
}
