using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class MockTaskServer : ITaskServer
    {
        public Func<Int32, CancellationToken, Task<String>> _CreateAgentSessionAsync { get; set; }
        public Func<Int32, String, AgentMessage, CancellationToken, Task> _DeleteAgentMessageAsync { get; set; }
        public Func<Int32, String, CancellationToken, Task> _DeleteAgentSessionAsync { get; set; }
        public Func<Int32, String, Int32, CancellationToken, Task<AgentMessage>> _GetAgentMessageAsync { get; set; }

        public Task<String> CreateAgentSessionAsync(Int32 poolId, CancellationToken cancellationToken)
        {
            return this._CreateAgentSessionAsync != null ? this._CreateAgentSessionAsync(poolId, cancellationToken) : Task.FromResult(default(String));
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, String sessionId, AgentMessage message, CancellationToken cancellationToken)
        {
            return this._DeleteAgentMessageAsync != null ? this._DeleteAgentMessageAsync(poolId, sessionId, message, cancellationToken) : Task.CompletedTask;
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, String sessionId, CancellationToken cancellationToken)
        {
            return this._DeleteAgentSessionAsync != null ? this._DeleteAgentSessionAsync(poolId, sessionId, cancellationToken) : Task.CompletedTask;
        }

        public Task<AgentMessage> GetAgentMessageAsync(Int32 poolId, String sessionId, Int32 lastMessageId, CancellationToken cancellationToken)
        {
            return this._GetAgentMessageAsync != null ? this._GetAgentMessageAsync(poolId, sessionId, lastMessageId, cancellationToken) : Task.FromResult(default(AgentMessage));
        }
    }
}