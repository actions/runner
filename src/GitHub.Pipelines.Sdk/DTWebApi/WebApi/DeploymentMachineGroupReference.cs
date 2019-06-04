using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class DeploymentMachineGroupReference
    {
        [DataMember(EmitDefaultValue = false)]
        public Int32 Id
        {
            get;
            internal set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ProjectReference Project
        {
            get;
            internal set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }
    }
}
