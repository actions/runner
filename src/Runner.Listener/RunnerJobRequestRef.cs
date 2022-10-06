using System.Runtime.Serialization;

namespace GitHub.Runner.Listener
{
    [DataContract]
    public sealed class RunnerJobRequestRef
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "runner_request_id")]
        public string RunnerRequestId { get; set; }
        [DataMember(Name = "run_service_url")]
        public string RunServiceUrl { get; set; }
    }
}
