using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class MockTaskServer : ITaskServer
    {
        public Func<Int32, TaskAgentSession, CancellationToken, Task<TaskAgentSession>> _CreateAgentSessionAsync { get; set; }
        public Func<Int32, Int64, Guid, CancellationToken, Task> _DeleteAgentMessageAsync { get; set; }
        public Func<Int32, Guid, CancellationToken, Task> _DeleteAgentSessionAsync { get; set; }
        public Func<Int32, Guid, Int64?, CancellationToken, Task<TaskAgentMessage>> _GetAgentMessageAsync { get; set; }

        public Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            return this._CreateAgentSessionAsync != null ? this._CreateAgentSessionAsync(poolId, session, cancellationToken) : Task.FromResult(default(TaskAgentSession));
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken)
        {
            return this._DeleteAgentMessageAsync != null ? this._DeleteAgentMessageAsync(poolId, messageId, sessionId, cancellationToken) : Task.CompletedTask;
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken)
        {
            return this._DeleteAgentSessionAsync != null ? this._DeleteAgentSessionAsync(poolId, sessionId, cancellationToken) : Task.CompletedTask;
        }

        public Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            return this._GetAgentMessageAsync != null ? this._GetAgentMessageAsync(poolId, sessionId, lastMessageId, cancellationToken) : Task.FromResult(default(TaskAgentMessage));
        }
    }
}