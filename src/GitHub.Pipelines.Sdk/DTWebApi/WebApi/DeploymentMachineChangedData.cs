using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class DeploymentMachineChangedData: DeploymentMachine, ICloneable
    {
        public DeploymentMachineChangedData()
        {
        }

        private DeploymentMachineChangedData(DeploymentMachineChangedData machineToBeCloned)
        {
            this.Id = machineToBeCloned.Id;
            this.Tags = (machineToBeCloned.Tags == null) ? null : new List<string>(machineToBeCloned.Tags);
            this.Agent = machineToBeCloned.Agent?.Clone();
            this.TagsAdded = (machineToBeCloned.TagsAdded == null) ? null : new List<string>(machineToBeCloned.TagsAdded);
            this.TagsDeleted = (machineToBeCloned.TagsDeleted == null) ? null : new List<string>(machineToBeCloned.TagsDeleted);
        }

        public DeploymentMachineChangedData(DeploymentMachine deploymentMachine)
        {
            this.Id = deploymentMachine.Id;
            this.Tags = deploymentMachine.Tags;
            this.Agent = deploymentMachine.Agent;
        }

        public IList<String> TagsAdded
        {
            get
            {
                return m_addedTags;
            }
            set
            {
                m_addedTags = value;
            }
        }

        public IList<String> TagsDeleted
        {
            get
            {
                return m_deletedTags;
            }
            set
            {
                m_deletedTags = value;
            }
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public new DeploymentMachineChangedData Clone()
        {
            return new DeploymentMachineChangedData(this);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "AddedTags")]
        private IList<String> m_addedTags;

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "DeletedTags")]
        private IList<String> m_deletedTags;
    }
}
