using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class BrokerWebSocketProbeResult
    {
        [JsonConstructor]
        public BrokerWebSocketProbeResult()
        {
            Errors = new List<string>();
        }

        [DataMember(Order = 1, EmitDefaultValue = true)]
        public bool Connected { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public int ConnectCount { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public int ConnectFailures { get; set; }

        [DataMember(Order = 4, EmitDefaultValue = false)]
        public int PingsReceived { get; set; }

        [DataMember(Order = 5, EmitDefaultValue = false)]
        public string LastCloseReason { get; set; }

        [DataMember(Order = 6, EmitDefaultValue = false)]
        public long TotalDurationMs { get; set; }

        [DataMember(Order = 7, EmitDefaultValue = false)]
        public List<string> Errors { get; set; }
    }
}
