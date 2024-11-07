using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ServiceConnectivityCheckInput
    {
        [JsonConstructor]
        public ServiceConnectivityCheckInput()
        {
            Endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Endpoints { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int IntervalInSecond { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int RequestTimeoutInSecond { get; set; }
    }

    [DataContract]
    public class ServiceConnectivityCheckResult
    {
        [JsonConstructor]
        public ServiceConnectivityCheckResult()
        {
            EndpointsResult = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        [DataMember(Order = 1, EmitDefaultValue = false)]
        public bool HasFailure { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public Dictionary<string, List<string>> EndpointsResult { get; set; }
    }
}
