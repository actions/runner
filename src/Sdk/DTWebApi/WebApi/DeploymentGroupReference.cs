using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment group reference. This is useful for referring a deployment group in another object.
    /// </summary>
    [DataContract]
    public class DeploymentGroupReference
    {
        [JsonConstructor]
        public DeploymentGroupReference()
        {
        }

        private DeploymentGroupReference(DeploymentGroupReference referenceToClone)
        {
            this.Id = referenceToClone.Id;
            this.Name = referenceToClone.Name;

            if (referenceToClone.Project != null)
            {
                this.Project = new ProjectReference
                {
                    Id = referenceToClone.Project.Id,
                    Name = referenceToClone.Project.Name,
                };
            }

            if (referenceToClone.Pool != null)
            {
                this.Pool = new TaskAgentPoolReference
                {
                    Id = referenceToClone.Pool.Id,
                    IsHosted = referenceToClone.Pool.IsHosted,
                    Name = referenceToClone.Pool.Name,
                    PoolType = referenceToClone.Pool.PoolType,
                    Scope = referenceToClone.Pool.Scope,
                };
            }
        }

        /// <summary>
        /// Deployment group identifier.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            internal set;
        }

        /// <summary>
        /// Project to which the deployment group belongs.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ProjectReference Project
        {
            get;
            internal set;
        }

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
        /// Deployment pool in which deployment agents are registered.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }

        public virtual DeploymentGroupReference Clone()
        {
            return new DeploymentGroupReference(this);
        }
    }
}
