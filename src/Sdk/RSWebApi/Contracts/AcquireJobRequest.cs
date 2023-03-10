using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class AcquireJobRequest
    {
        [DataMember(Name = "streamId", EmitDefaultValue = false)]
        public string StreamID { get; set; }
    }
}