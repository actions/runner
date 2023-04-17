using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class AcquireJobRequest
    {
        [DataMember(Name = "jobMessageId", EmitDefaultValue = false)]
        public string JobMessageID { get; set; }
    }
}
