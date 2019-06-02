using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentJobStep
    {
        [DataContract]
        public enum TaskAgentJobStepType
        {
            [DataMember]
            Task = 1,

            [DataMember]
            Action = 2
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskAgentJobStepType Type
        {
            get;
            set;
        }

        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember]
        public Boolean Enabled
        {
            get;
            set;
        }

        [DataMember]
        public String Condition
        {
            get;
            set;
        }

        [DataMember]
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        [DataMember]
        public Int32 TimeoutInMinutes
        {
            get;
            set;
        }

        [DataMember]
        public TaskAgentJobTask Task
        {
            get;
            set;
        }

        [DataMember]
        public IDictionary<String, String> Env
        {
            get;
            set;
        }

        [DataMember]
        public IDictionary<String, String> Inputs
        {
            get;
            set;
        }
    }
}
