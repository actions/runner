using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITaskServer
    {
        Task<String> CreateAgentSession(Int32 poolId);
        Task DeleteAgentMessage(Int32 poolId, String sessionId, AgentMessage message);
        Task DeleteAgentSession(Int32 poolId, String sessionId);
        Task<AgentMessage> GetAgentMessage(Int32 poolId, String sessionId, Int32 lastMessageId);
    }

    public sealed class TaskServer : ITaskServer
    {
        public Task<String> CreateAgentSession(Int32 poolId)
        {
            // TODO: Pass through to the REST SDK.
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentMessage(Int32 poolId, String sessionId, AgentMessage message)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentSession(Int32 poolId, String sessionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<AgentMessage> GetAgentMessage(Int32 poolId, String sessionId, Int32 lastMessageId)
        {
            throw new System.NotImplementedException();
        }
    }
}