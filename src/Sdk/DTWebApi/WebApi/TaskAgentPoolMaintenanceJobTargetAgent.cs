using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentPoolMaintenanceJobTargetAgent
    {
        internal TaskAgentPoolMaintenanceJobTargetAgent()
        {
        }

        [DataMember]
        public Int32 JobId
        {
            get;
            set;
        }

        [DataMember]
        public TaskAgentReference Agent
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolMaintenanceJobStatus? Status
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolMaintenanceJobResult? Result
        {
            get;
            set;
        }
    }
}
