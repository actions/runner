using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITaskServer
    {
        Task<String> CreateAgentSessionAsync(Int32 poolId);
        Task DeleteAgentMessageAsync(Int32 poolId, String sessionId, AgentMessage message);
        Task DeleteAgentSessionAsync(Int32 poolId, String sessionId);
        Task<AgentMessage> GetAgentMessageAsync(Int32 poolId, String sessionId, Int32 lastMessageId);
    }

    public sealed class TaskServer : ITaskServer
    {
        public Task<String> CreateAgentSessionAsync(Int32 poolId)
        {
            // TODO: Pass through to the REST SDK.
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, String sessionId, AgentMessage message)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, String sessionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<AgentMessage> GetAgentMessageAsync(Int32 poolId, String sessionId, Int32 lastMessageId)
        {
            throw new System.NotImplementedException();
        }
    }
}