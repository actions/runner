using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent
{
    public enum AgentConnectionType
    {
        Generic,
        MessageQueue,
        JobRequest
    }

    [ServiceLocator(Default = typeof(AgentServer))]
    public interface IAgentServer : IAgentService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task RefreshConnectionAsync(AgentConnectionType connectionType, TimeSpan timeout);

        void SetConnectionTimeout(AgentConnectionType connectionType, TimeSpan timeout);

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
        private bool _hasGenericConnection;
        private bool _hasMessageConnection;
        private bool _hasRequestConnection;
        private VssConnection _genericConnection;
        private VssConnection _messageConnection;
        private VssConnection _requestConnection;
        private TaskAgentHttpClient _genericTaskAgentClient;
        private TaskAgentHttpClient _messageTaskAgentClient;
        private TaskAgentHttpClient _requestTaskAgentClient;

        public async Task ConnectAsync(Uri serverUrl, VssCredentials credentials)
        {
            if (HostContext.RunMode == RunMode.Local)
            {
                return;
            }

            _genericConnection = await EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(100));
            _messageConnection = await EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(60));
            _requestConnection = await EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(60));

            _genericTaskAgentClient = _genericConnection.GetClient<TaskAgentHttpClient>();
            _messageTaskAgentClient = _messageConnection.GetClient<TaskAgentHttpClient>();
            _requestTaskAgentClient = _requestConnection.GetClient<TaskAgentHttpClient>();

            _hasGenericConnection = true;
            _hasMessageConnection = true;
            _hasRequestConnection = true;
        }

        // Refresh connection is best effort. it should never throw exception
        public async Task RefreshConnectionAsync(AgentConnectionType connectionType, TimeSpan timeout)
        {
            Trace.Info($"Refresh {connectionType} VssConnection to get on a different AFD node.");
            VssConnection newConnection = null;
            switch (connectionType)
            {
                case AgentConnectionType.MessageQueue:
                    try
                    {
                        _hasMessageConnection = false;
                        newConnection = await EstablishVssConnection(_messageConnection.Uri, _messageConnection.Credentials, timeout);
                        var client = newConnection.GetClient<TaskAgentHttpClient>();
                        _messageConnection = newConnection;
                        _messageTaskAgentClient = client;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Catch exception during reset {connectionType} connection.");
                        Trace.Error(ex);
                        newConnection?.Dispose();
                    }
                    finally
                    {
                        _hasMessageConnection = true;
                    }
                    break;
                case AgentConnectionType.JobRequest:
                    try
                    {
                        _hasRequestConnection = false;
                        newConnection = await EstablishVssConnection(_requestConnection.Uri, _requestConnection.Credentials, timeout);
                        var client = newConnection.GetClient<TaskAgentHttpClient>();
                        _requestConnection = newConnection;
                        _requestTaskAgentClient = client;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Catch exception during reset {connectionType} connection.");
                        Trace.Error(ex);
                        newConnection?.Dispose();
                    }
                    finally
                    {
                        _hasRequestConnection = true;
                    }
                    break;
                case AgentConnectionType.Generic:
                    try
                    {
                        _hasGenericConnection = false;
                        newConnection = await EstablishVssConnection(_genericConnection.Uri, _genericConnection.Credentials, timeout);
                        var client = newConnection.GetClient<TaskAgentHttpClient>();
                        _genericConnection = newConnection;
                        _genericTaskAgentClient = client;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Catch exception during reset {connectionType} connection.");
                        Trace.Error(ex);
                        newConnection?.Dispose();
                    }
                    finally
                    {
                        _hasGenericConnection = true;
                    }
                    break;
                default:
                    Trace.Error($"Unexpected connection type: {connectionType}.");
                    break;
            }
        }

        public void SetConnectionTimeout(AgentConnectionType connectionType, TimeSpan timeout)
        {
            Trace.Info($"Set {connectionType} VssConnection's timeout to {timeout.TotalSeconds} seconds.");
            switch (connectionType)
            {
                case AgentConnectionType.JobRequest:
                    _requestConnection.Settings.SendTimeout = timeout;
                    break;
                case AgentConnectionType.MessageQueue:
                    _messageConnection.Settings.SendTimeout = timeout;
                    break;
                case AgentConnectionType.Generic:
                    _genericConnection.Settings.SendTimeout = timeout;
                    break;
                default:
                    Trace.Error($"Unexpected connection type: {connectionType}.");
                    break;
            }
        }

        private async Task<VssConnection> EstablishVssConnection(Uri serverUrl, VssCredentials credentials, TimeSpan timeout)
        {
            Trace.Info($"Establish connection with {timeout.TotalSeconds} seconds timeout.");
            int attemptCount = 5;
            while (attemptCount-- > 0)
            {
                var connection = VssUtil.CreateConnection(serverUrl, credentials, timeout: timeout);
                try
                {
                    await connection.ConnectAsync();
                    return connection;
                }
                catch (Exception ex) when (attemptCount > 0)
                {
                    Trace.Info($"Catch exception during connect. {attemptCount} attempt left.");
                    Trace.Error(ex);

                    await HostContext.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                }
            }

            // should never reach here.
            throw new InvalidOperationException(nameof(EstablishVssConnection));
        }

        private void CheckConnection(AgentConnectionType connectionType)
        {
            switch (connectionType)
            {
                case AgentConnectionType.Generic:
                    if (!_hasGenericConnection)
                    {
                        throw new InvalidOperationException($"SetConnection {AgentConnectionType.Generic}");
                    }
                    break;
                case AgentConnectionType.JobRequest:
                    if (!_hasRequestConnection)
                    {
                        throw new InvalidOperationException($"SetConnection {AgentConnectionType.JobRequest}");
                    }
                    break;
                case AgentConnectionType.MessageQueue:
                    if (!_hasMessageConnection)
                    {
                        throw new InvalidOperationException($"SetConnection {AgentConnectionType.MessageQueue}");
                    }
                    break;
                default:
                    throw new NotSupportedException(connectionType.ToString());
            }
        }

        //-----------------------------------------------------------------
        // Configuration
        //-----------------------------------------------------------------

        public Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName, TaskAgentPoolType poolType = TaskAgentPoolType.Automation)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.GetAgentPoolsAsync(agentPoolName, poolType: poolType);
        }

        public Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.AddAgentAsync(agentPoolId, agent);
        }

        public Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.GetAgentsAsync(agentPoolId, agentName, false);
        }

        public Task<TaskAgent> UpdateAgentAsync(int agentPoolId, TaskAgent agent)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.ReplaceAgentAsync(agentPoolId, agent);
        }

        public Task DeleteAgentAsync(int agentPoolId, int agentId)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.DeleteAgentAsync(agentPoolId, agentId);
        }

        //-----------------------------------------------------------------
        // MessageQueue
        //-----------------------------------------------------------------

        public Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection(AgentConnectionType.MessageQueue);
            return _messageTaskAgentClient.CreateAgentSessionAsync(poolId, session, cancellationToken: cancellationToken);
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection(AgentConnectionType.MessageQueue);
            return _messageTaskAgentClient.DeleteMessageAsync(poolId, messageId, sessionId, cancellationToken: cancellationToken);
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection(AgentConnectionType.MessageQueue);
            return _messageTaskAgentClient.DeleteAgentSessionAsync(poolId, sessionId, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            CheckConnection(AgentConnectionType.MessageQueue);
            return _messageTaskAgentClient.GetMessageAsync(poolId, sessionId, lastMessageId, cancellationToken: cancellationToken);
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

            CheckConnection(AgentConnectionType.JobRequest);
            return _requestTaskAgentClient.RenewAgentRequestAsync(poolId, requestId, lockToken, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentJobRequest> FinishAgentRequestAsync(int poolId, long requestId, Guid lockToken, DateTime finishTime, TaskResult result, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (HostContext.RunMode == RunMode.Local)
            {
                return Task.FromResult<TaskAgentJobRequest>(null);
            }

            CheckConnection(AgentConnectionType.JobRequest);
            return _requestTaskAgentClient.FinishAgentRequestAsync(poolId, requestId, lockToken, finishTime, result, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentJobRequest> GetAgentRequestAsync(int poolId, long requestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            CheckConnection(AgentConnectionType.JobRequest);
            return _requestTaskAgentClient.GetAgentRequestAsync(poolId, requestId, cancellationToken: cancellationToken);
        }

        //-----------------------------------------------------------------
        // Agent Package
        //-----------------------------------------------------------------
        public Task<List<PackageMetadata>> GetPackagesAsync(string packageType, string platform, int top, CancellationToken cancellationToken)
        {
            ArgUtil.Equal(RunMode.Normal, HostContext.RunMode, nameof(HostContext.RunMode));
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.GetPackagesAsync(packageType, platform, top, cancellationToken: cancellationToken);
        }

        public Task<PackageMetadata> GetPackageAsync(string packageType, string platform, string version, CancellationToken cancellationToken)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.GetPackageAsync(packageType, platform, version, cancellationToken: cancellationToken);
        }

        public Task<TaskAgent> UpdateAgentUpdateStateAsync(int agentPoolId, int agentId, string currentState)
        {
            CheckConnection(AgentConnectionType.Generic);
            return _genericTaskAgentClient.UpdateAgentUpdateStateAsync(agentPoolId, agentId, currentState);
        }
    }
}