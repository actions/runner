using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    // TODO: Remove these classes after the VSTS Rest SDK references are added.
    public sealed class AgentMessage
    {
        public Int32 MessageId { get; set; }
        public String MessageType { get; set; }
    }

    public static class AgentRefreshMessage
    {
        public static readonly String MessageType = "Some refresh message type";
    }

    public sealed class TaskAgentNotFoundException : System.Exception
    {
        public TaskAgentNotFoundException() { }
        public TaskAgentNotFoundException( string message ) : base( message ) { }
        public TaskAgentNotFoundException( string message, System.Exception inner ) : base( message, inner ) { }
    }

    public sealed class TaskAgentSessionConflictException : System.Exception
    {
        public TaskAgentSessionConflictException() { }
        public TaskAgentSessionConflictException( string message ) : base( message ) { }
        public TaskAgentSessionConflictException( string message, System.Exception inner ) : base( message, inner ) { }
    }

    public class TaskAgentSessionExpiredException : System.Exception
    {
        public TaskAgentSessionExpiredException() { }
        public TaskAgentSessionExpiredException( string message ) : base( message ) { }
        public TaskAgentSessionExpiredException( string message, System.Exception inner ) : base( message, inner ) { }
    }
}