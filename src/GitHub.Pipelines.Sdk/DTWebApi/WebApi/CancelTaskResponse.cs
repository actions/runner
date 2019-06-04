using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.WebApi
{
    [ClientIgnore]
    [DataContract]
    public sealed class CancelTaskResponse
    {
        [DataMember]
        public Boolean WaitForLocalCancellationComplete
        {
            get;
            set;
        }
    }
}
