using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentJobVariable
    {
        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember]
        public String Value
        {
            get;
            set;
        }

        [DataMember]
        public Boolean Secret
        {
            get;
            set;
        }
    }
}
