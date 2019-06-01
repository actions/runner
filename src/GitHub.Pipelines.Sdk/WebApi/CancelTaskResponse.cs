using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
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