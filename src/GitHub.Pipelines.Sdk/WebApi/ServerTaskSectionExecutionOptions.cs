using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [ClientIgnore]
    [DataContract]
    public class ServerTaskSectionExecutionOptions
    {
        [DataMember]
        public Boolean IsRetryable { get; set; }

        [DataMember]
        public TimeSpan? DelayBetweenRetries { get; set; }
    }
}
