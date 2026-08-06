using System;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Raised when the debugger's Dev Tunnel relay connection cannot be established
    /// or is lost while the job is running.
    /// </summary>
    /// <remarks>
    /// This is a dedicated type so callers can tell a broken tunnel (an infrastructure
    /// failure the user cannot do anything about) apart from a user simply never
    /// attaching a debug client, which surfaces as a <see cref="TimeoutException"/>.
    /// </remarks>
    public sealed class DebuggerTunnelException : Exception
    {
        public DebuggerTunnelException(string message)
            : base(message)
        {
        }

        public DebuggerTunnelException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
