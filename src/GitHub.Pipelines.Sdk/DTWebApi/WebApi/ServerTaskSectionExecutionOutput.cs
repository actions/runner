using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [ClientIgnore]
    public class ServerTaskSectionExecutionOutput
    {
        [DataMember]
        public Boolean? IsCompleted { get; set; }
    }
}
