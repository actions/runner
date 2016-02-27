using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(TaskServer))]
    public interface ITaskServer: IAgentService
    {
        // messagequeue
        Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken);
        Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken);
        Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken);
        
        // logging and console
        void QueueWebConsoleLine(Guid timeLineId, string line);
    }

    public sealed class TaskServer : AgentService, ITaskServer
    {
        //-----------------------------------------------------------------
        // MessageQueue
        //-----------------------------------------------------------------
                
        public async Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            // TODO: Pass through to the REST SDK.
            await Task.Yield();
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
            var result = new TaskAgentMessage();
            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = null;
            JobEnvironment environment = new JobEnvironment();
            List<TaskInstance> tasks = new List<TaskInstance>();
            Guid JobId = Guid.NewGuid();
            var jobRequest = new JobRequestMessage(plan, timeline, JobId, "someJob", environment, tasks);
            result.Body = JsonUtility.ToString(jobRequest);
            result.MessageType = JobRequestMessage.MessageType;
            result.MessageId = 123;
            await Task.Yield();
            return result;
        }
        
        //-----------------------------------------------------------------
        // Feedback: WebConsole and Logs
        //-----------------------------------------------------------------
        
        public void QueueWebConsoleLine(Guid timeLineId, string line)
        {
            // TODO: queue line
            Console.WriteLine(StringUtil.Format("Console: {0}", line));
        }
    }
}