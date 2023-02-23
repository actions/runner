using System;
using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class RenewJobResponse
    {
        [DataMember]
        public DateTime LockedUntil
        {
            get;
            internal set;
        }
    }
}