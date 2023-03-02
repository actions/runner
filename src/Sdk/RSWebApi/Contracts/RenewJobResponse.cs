using System;
using System.Runtime.Serialization;

namespace Sdk.RSWebApi.Contracts
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
