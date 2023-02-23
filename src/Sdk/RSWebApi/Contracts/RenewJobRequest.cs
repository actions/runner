using System;
using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class RenewJobRequest
    {
        [DataMember(Name = "planId", EmitDefaultValue = false)]
        public Guid PlanID { get; set; }
        
        [DataMember(Name = "jobId", EmitDefaultValue = false)]
        public Guid JobID { get; set; }
    }
}