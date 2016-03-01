using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
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
        void SetConnection(VssConnection connection);
        Task ConnectAsync();
        
        // messagequeue
        Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken);
        Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken);
        Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken);
        
        // registration
        Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName);
        Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null);
        Task DeleteAgentAsync(int agentPoolId, int agentId);
        Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent);
        Task<TaskAgent> UpdateAgentAsync(int agentPoolId, TaskAgent agent);
        
        // logging and console
        void QueueWebConsoleLine(Guid timeLineId, string line);
    }

    public sealed class TaskServer : AgentService, ITaskServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskAgentHttpClient _taskClient;
         
        public void SetConnection(VssConnection connection)
        {
            _taskClient = connection.GetClient<TaskAgentHttpClient>();
            _connection = connection;
            _hasConnection = true;
        }
        
        public Task ConnectAsync()
        {
            return _connection.ConnectAsync();
        }
        
        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }
                
        //-----------------------------------------------------------------
        // Configuration
        //-----------------------------------------------------------------

        public Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName)
        {
            CheckConnection();
            return _taskClient.GetAgentPoolsAsync(agentPoolName);
        }

        public Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent)
        {
            CheckConnection();
            return _taskClient.AddAgentAsync(agentPoolId, agent);
        }

        public Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null)
        {
            CheckConnection();
            return _taskClient.GetAgentsAsync(agentPoolId, agentName, false);
        }
        
        public Task<TaskAgent> UpdateAgentAsync(int agentPoolId, TaskAgent agent)
        {
            CheckConnection();
            return _taskClient.ReplaceAgentAsync(agentPoolId, agent);
        }

        public Task DeleteAgentAsync(int agentPoolId, int agentId)
        {
            CheckConnection();
            return _taskClient.DeleteAgentAsync(agentPoolId, agentId);
        }

        //-----------------------------------------------------------------
        // MessageQueue
        //-----------------------------------------------------------------

        public Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateAgentSessionAsync(poolId, session, null, cancellationToken);
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.DeleteMessageAsync(poolId, messageId, sessionId, null, cancellationToken);
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.DeleteAgentSessionAsync(poolId, sessionId, null, cancellationToken);
        }

        public Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            CheckConnection();
            var task = _taskClient.GetMessageAsync(poolId, sessionId, lastMessageId, cancellationToken);
            //TODO: find out why GetMessageAsync does not respect the cancellation token that is passed
            //in the mean time use this workaround
            return task.WithCancellation(cancellationToken);
        }

        //-----------------------------------------------------------------
        // Feedback: WebConsole and Logs
        //-----------------------------------------------------------------

        public void QueueWebConsoleLine(Guid timeLineId, string line)
        {
            CheckConnection();
            
            // TODO: queue line
            Console.WriteLine(StringUtil.Format("Console: {0}", line));
        }
    }
}