using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Information about a job run on the runner
    /// </summary>
    [DataContract]
    public class JobTelemetry
    {
        [DataMember(EmitDefaultValue = false)]
        public string Message { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public JobTelemetryType Type { get; set; }
    }
}
