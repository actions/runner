using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITaskServer
    {
        Task<String> CreateAgentSessionAsync(Int32 poolId, CancellationToken cancellationToken);
        Task DeleteAgentMessageAsync(Int32 poolId, String sessionId, AgentMessage message, CancellationToken cancellationToken);
        Task DeleteAgentSessionAsync(Int32 poolId, String sessionId, CancellationToken cancellationToken);
        Task<AgentMessage> GetAgentMessageAsync(Int32 poolId, String sessionId, Int32 lastMessageId, CancellationToken cancellationToken);
    }

    public sealed class TaskServer : ITaskServer
    {
        public Task<String> CreateAgentSessionAsync(Int32 poolId, CancellationToken cancellationToken)
        {
            // TODO: Pass through to the REST SDK.
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, String sessionId, AgentMessage message, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, String sessionId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<AgentMessage> GetAgentMessageAsync(Int32 poolId, String sessionId, Int32 lastMessageId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}