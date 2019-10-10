using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TimelineReference
    {
        public TimelineReference()
        {
        }

        [DataMember(Order = 1)]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember(Order = 2)]
        public Int32 ChangeId
        {
            get;
            set;
        }

        [DataMember(Order = 3)]
        public Uri Location
        {
            get;
            set;
        }
    }
}
