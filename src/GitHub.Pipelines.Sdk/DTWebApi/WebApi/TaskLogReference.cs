using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskLogReference
    {
        [DataMember]
        public Int32 Id
        {
            get;
            set;
        }

        [DataMember]
        public Uri Location
        {
            get;
            set;
        }
    }
}
