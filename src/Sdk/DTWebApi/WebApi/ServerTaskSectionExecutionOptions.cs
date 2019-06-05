using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.WebApi
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
