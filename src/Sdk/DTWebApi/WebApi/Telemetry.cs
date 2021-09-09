using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Information about a job run on the runner
    /// </summary>
    [DataContract]
    public class Telemetry
    {
        [DataMember(EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Message { get; set; }
    }
}
