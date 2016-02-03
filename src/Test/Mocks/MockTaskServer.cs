using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class MockTaskServer : ITaskServer
    {
        public Func<Int32, Task<String>> _CreateAgentSession { get; set; }
        public Func<Int32, String, AgentMessage, Task> _DeleteAgentMessage { get; set; }
        public Func<Int32, String, Task> _DeleteAgentSession { get; set; }
        public Func<Int32, String, Int32, Task<AgentMessage>> _GetAgentMessage { get; set; }

        public Task<String> CreateAgentSession(Int32 poolId)
        {
            return this._CreateAgentSession != null ? this._CreateAgentSession(poolId) : Task.FromResult(default(String));
        }

        public Task DeleteAgentMessage(Int32 poolId, String sessionId, AgentMessage message)
        {
            return this._DeleteAgentMessage != null ? this._DeleteAgentMessage(poolId, sessionId, message) : Task.CompletedTask;
        }

        public Task DeleteAgentSession(Int32 poolId, String sessionId)
        {
            return this._DeleteAgentSession != null ? this._DeleteAgentSession(poolId, sessionId) : Task.CompletedTask;
        }

        public Task<AgentMessage> GetAgentMessage(Int32 poolId, String sessionId, Int32 lastMessageId)
        {
            return this._GetAgentMessage != null ? this._GetAgentMessage(poolId, sessionId, lastMessageId) : Task.FromResult(default(AgentMessage));
        }
    }
}