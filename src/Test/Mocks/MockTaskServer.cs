using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class MockTaskServer : ITaskServer
    {
        public Func<Int32, Task<String>> _CreateAgentSessionAsync { get; set; }
        public Func<Int32, String, AgentMessage, Task> _DeleteAgentMessageAsync { get; set; }
        public Func<Int32, String, Task> _DeleteAgentSessionAsync { get; set; }
        public Func<Int32, String, Int32, Task<AgentMessage>> _GetAgentMessageAsync { get; set; }

        public Task<String> CreateAgentSessionAsync(Int32 poolId)
        {
            return this._CreateAgentSessionAsync != null ? this._CreateAgentSessionAsync(poolId) : Task.FromResult(default(String));
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, String sessionId, AgentMessage message)
        {
            return this._DeleteAgentMessageAsync != null ? this._DeleteAgentMessageAsync(poolId, sessionId, message) : Task.CompletedTask;
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, String sessionId)
        {
            return this._DeleteAgentSessionAsync != null ? this._DeleteAgentSessionAsync(poolId, sessionId) : Task.CompletedTask;
        }

        public Task<AgentMessage> GetAgentMessageAsync(Int32 poolId, String sessionId, Int32 lastMessageId)
        {
            return this._GetAgentMessageAsync != null ? this._GetAgentMessageAsync(poolId, sessionId, lastMessageId) : Task.FromResult(default(AgentMessage));
        }
    }
}