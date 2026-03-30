using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Dev Tunnel information the runner needs to host the debugger tunnel.
    /// Matches the run-service <c>DebuggerTunnel</c> contract.
    /// </summary>
    [DataContract]
    public sealed class DebuggerTunnelInfo
    {
        [DataMember(EmitDefaultValue = false)]
        public string TunnelId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ClusterId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string HostToken { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ushort Port { get; set; }
    }
}
