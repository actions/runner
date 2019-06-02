using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    [ClientIgnore]
    public class ServerTaskSectionExecutionOutput
    {
        [DataMember]
        public Boolean? IsCompleted { get; set; }
    }
}
