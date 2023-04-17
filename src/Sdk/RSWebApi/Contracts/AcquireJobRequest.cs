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

        // The earliest versions of this type serialized the JobMessageID under a field named "streamId"
        // This private property is expected to be a short-lived shim to aid in transitioning over to the new schema.
        [DataMember(Name = "streamId", EmitDefaultValue = false)]
        private string DeprecatedJobMessageIdAlias
        {
            get
            {
                return this.JobMessageID;
            }
            set
            {
                // Note that this is a private property, so the only way to reach this setter is via deserialization.
                // Deserializtion is NOT an expected use case on the Runner side as of early 2023.
                // In the unlikely event that deserializtion does become a valid use case, we include this guard
                // to ensure that a valid JobMessageID can't be overwritten by this otherwise unreachable code path.
                if (string.IsNullOrEmpty(this.JobMessageID))
                {
                    this.JobMessageID = value;
                }
            }
        }
    }
}