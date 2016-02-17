using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(TaskServer))]
    public interface ITaskServer
    {
        Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken);
        Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken);
        Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken);
    }

    public sealed class TaskServer : ITaskServer
    {
        public async Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            // TODO: Pass through to the REST SDK.
            //throw new System.NotImplementedException();
            return session;
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            //throw new System.NotImplementedException();
            var result = new TaskAgentMessage();                        
            //var jobRequest = new JobRequestMessage(null, null, Guid.Empty, "someJob", null, null);
            //result.Body = JsonUtility.ToString(jobRequest);
            result.Body = "some text";
            result.MessageType = JobRequestMessage.MessageType;
            result.MessageId = 123;
            return result;
        }
    }
}