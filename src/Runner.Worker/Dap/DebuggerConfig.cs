using GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Consolidated runtime configuration for the job debugger.
    /// Populated once from the acquire response and owned by <see cref="GlobalContext"/>.
    /// </summary>
    public sealed class DebuggerConfig
    {
        public DebuggerConfig(bool enabled, DebuggerTunnelInfo tunnel)
        {
            Enabled = enabled;
            Tunnel = tunnel;
        }

        /// <summary>Whether the debugger is enabled for this job.</summary>
        public bool Enabled { get; }

        /// <summary>
        /// Dev Tunnel details for remote debugging.
        /// Required when <see cref="Enabled"/> is true.
        /// </summary>
        public DebuggerTunnelInfo Tunnel { get; }

        /// <summary>Whether the tunnel configuration is complete and valid.</summary>
        public bool HasValidTunnel => Tunnel != null
            && !string.IsNullOrEmpty(Tunnel.TunnelId)
            && !string.IsNullOrEmpty(Tunnel.ClusterId)
            && !string.IsNullOrEmpty(Tunnel.HostToken)
            && Tunnel.Port >= 1024 && Tunnel.Port <= 65535;
    }
}
