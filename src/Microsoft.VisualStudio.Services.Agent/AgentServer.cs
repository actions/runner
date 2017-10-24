using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(AgentServer))]
    public interface IAgentServer : IAgentService
    {
        Task ConnectAsync(VssConnection agentConnection);

        // Configuration
        Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent);
        Task DeleteAgentAsync(int agentPoolId, int agentId);
        Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName, TaskAgentPoolType poolType = TaskAgentPoolType.Automation);
        Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null);
        Task<TaskAgent> UpdateAgentAsync(int agentPoolId, TaskAgent agent);

        // messagequeue
        Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken);
        Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken);
        Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken);

        // job request
        Task<TaskAgentJobRequest> GetAgentRequestAsync(int poolId, long requestId, CancellationToken cancellationToken);
        Task<TaskAgentJobRequest> RenewAgentRequestAsync(int poolId, long requestId, Guid lockToken, CancellationToken cancellationToken);
        Task<TaskAgentJobRequest> FinishAgentRequestAsync(int poolId, long requestId, Guid lockToken, DateTime finishTime, TaskResult result, CancellationToken cancellationToken);

        // agent package
        Task<List<PackageMetadata>> GetPackagesAsync(string packageType, string platform, int top, CancellationToken cancellationToken);
        Task<PackageMetadata> GetPackageAsync(string packageType, string platform, string version, CancellationToken cancellationToken);

        // agent update
        Task<TaskAgent> UpdateAgentUpdateStateAsync(int agentPoolId, int agentId, string currentState);
    }

    public sealed class AgentServer : AgentService, IAgentServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskAgentHttpClient _taskAgentClient;

        public async Task ConnectAsync(VssConnection agentConnection)
        {
            if (HostContext.RunMode == RunMode.Local)
            {
                return;
            }

            _connection = agentConnection;
            int attemptCount = 5;
            while (!_connection.HasAuthenticated && attemptCount-- > 0)
            {
                try
                {
                    await _connection.ConnectAsync();
                    break;
                }
                catch (Exception ex) when (attemptCount > 0)
                {
                    Trace.Info($"Catch exception during connect. {attemptCount} attemp left.");
                    Trace.Error(ex);
                }

                await Task.Delay(100);
            }

            _taskAgentClient = _connection.GetClient<TaskAgentHttpClient>();
            _hasConnection = true;
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

        public Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName, TaskAgentPoolType poolType = TaskAgentPoolType.Automation)
        {
            CheckConnection();
            return _taskAgentClient.GetAgentPoolsAsync(agentPoolName, poolType: poolType);
        }

        public Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent)
        {
            CheckConnection();
            return _taskAgentClient.AddAgentAsync(agentPoolId, agent);
        }

        public Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null)
        {
            CheckConnection();
            return _taskAgentClient.GetAgentsAsync(agentPoolId, agentName, false);
        }

        public Task<TaskAgent> UpdateAgentAsync(int agentPoolId, TaskAgent agent)
        {
            CheckConnection();
            return _taskAgentClient.ReplaceAgentAsync(agentPoolId, agent);
        }

        public Task DeleteAgentAsync(int agentPoolId, int agentId)
        {
            CheckConnection();
            return _taskAgentClient.DeleteAgentAsync(agentPoolId, agentId);
        }

        //-----------------------------------------------------------------
        // MessageQueue
        //-----------------------------------------------------------------

        public Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskAgentClient.CreateAgentSessionAsync(poolId, session, cancellationToken: cancellationToken);
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskAgentClient.DeleteMessageAsync(poolId, messageId, sessionId, cancellationToken: cancellationToken);
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskAgentClient.DeleteAgentSessionAsync(poolId, sessionId, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskAgentClient.GetMessageAsync(poolId, sessionId, lastMessageId, cancellationToken: cancellationToken);
        }

        //-----------------------------------------------------------------
        // JobRequest
        //-----------------------------------------------------------------

        public Task<TaskAgentJobRequest> RenewAgentRequestAsync(int poolId, long requestId, Guid lockToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (HostContext.RunMode == RunMode.Local)
            {
                return Task.FromResult(JsonUtility.FromString<TaskAgentJobRequest>("{ lockedUntil: \"" + DateTime.Now.Add(TimeSpan.FromMinutes(5)).ToString("u") + "\" }"));
            }

            CheckConnection();
            return _taskAgentClient.RenewAgentRequestAsync(poolId, requestId, lockToken, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentJobRequest> FinishAgentRequestAsync(int poolId, long requestId, Guid lockToken, DateTime finishTime, TaskResult result, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (HostContext.RunMode == RunMode.Local)
            {
                return Task.FromResult<TaskAgentJobRequest>(null);
            }

            CheckConnection();
            return _taskAgentClient.FinishAgentRequestAsync(poolId, requestId, lockToken, finishTime, result, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentJobRequest> GetAgentRequestAsync(int poolId, long requestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            CheckConnection();
            return _taskAgentClient.GetAgentRequestAsync(poolId, requestId, cancellationToken: cancellationToken);
        }

        //-----------------------------------------------------------------
        // Agent Package
        //-----------------------------------------------------------------
        public Task<List<PackageMetadata>> GetPackagesAsync(string packageType, string platform, int top, CancellationToken cancellationToken)
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            CheckConnection();
            return _taskAgentClient.GetPackagesAsync(packageType, platform, top, cancellationToken: cancellationToken);
        }

        public Task<PackageMetadata> GetPackageAsync(string packageType, string platform, string version, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskAgentClient.GetPackageAsync(packageType, platform, version, cancellationToken: cancellationToken);
        }

        public Task<TaskAgent> UpdateAgentUpdateStateAsync(int agentPoolId, int agentId, string currentState)
        {
            CheckConnection();
            return _taskAgentClient.UpdateAgentUpdateStateAsync(agentPoolId, agentId, currentState);
        }
    }
}