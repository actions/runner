using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common
{
    public enum RunnerConnectionType
    {
        Generic,
        MessageQueue,
        JobRequest
    }

    [ServiceLocator(Default = typeof(RunnerServer))]
    public interface IRunnerServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task RefreshConnectionAsync(RunnerConnectionType connectionType, TimeSpan timeout);

        void SetConnectionTimeout(RunnerConnectionType connectionType, TimeSpan timeout);

        // Configuration
        Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent);
        Task DeleteAgentAsync(int agentPoolId, int agentId);
        Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName = null, TaskAgentPoolType poolType = TaskAgentPoolType.Automation);
        Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null);
        Task<TaskAgent> ReplaceAgentAsync(int agentPoolId, TaskAgent agent);

        // messagequeue
        Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken);
        Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken);
        Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken);

        // job request
        Task<TaskAgentJobRequest> GetAgentRequestAsync(int poolId, long requestId, CancellationToken cancellationToken);
        Task<TaskAgentJobRequest> RenewAgentRequestAsync(int poolId, long requestId, Guid lockToken, string orchestrationId, CancellationToken cancellationToken);
        Task<TaskAgentJobRequest> FinishAgentRequestAsync(int poolId, long requestId, Guid lockToken, DateTime finishTime, TaskResult result, CancellationToken cancellationToken);

        // agent package
        Task<List<PackageMetadata>> GetPackagesAsync(string packageType, string platform, int top, CancellationToken cancellationToken);
        Task<PackageMetadata> GetPackageAsync(string packageType, string platform, string version, CancellationToken cancellationToken);

        // agent update
        Task<TaskAgent> UpdateAgentUpdateStateAsync(int agentPoolId, int agentId, string currentState);
    }

    public sealed class RunnerServer : RunnerService, IRunnerServer
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
            var createGenericConnection = EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(100));
            var createMessageConnection = EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(60));
            var createRequestConnection = EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(60));

            await Task.WhenAll(createGenericConnection, createMessageConnection, createRequestConnection);

            _genericConnection = await createGenericConnection;
            _messageConnection = await createMessageConnection;
            _requestConnection = await createRequestConnection;

            _genericTaskAgentClient = _genericConnection.GetClient<TaskAgentHttpClient>();
            _messageTaskAgentClient = _messageConnection.GetClient<TaskAgentHttpClient>();
            _requestTaskAgentClient = _requestConnection.GetClient<TaskAgentHttpClient>();

            _hasGenericConnection = true;
            _hasMessageConnection = true;
            _hasRequestConnection = true;
        }

        // Refresh connection is best effort. it should never throw exception
        public async Task RefreshConnectionAsync(RunnerConnectionType connectionType, TimeSpan timeout)
        {
            Trace.Info($"Refresh {connectionType} VssConnection to get on a different AFD node.");
            VssConnection newConnection = null;
            switch (connectionType)
            {
                case RunnerConnectionType.MessageQueue:
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
                case RunnerConnectionType.JobRequest:
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
                case RunnerConnectionType.Generic:
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

        public void SetConnectionTimeout(RunnerConnectionType connectionType, TimeSpan timeout)
        {
            Trace.Info($"Set {connectionType} VssConnection's timeout to {timeout.TotalSeconds} seconds.");
            switch (connectionType)
            {
                case RunnerConnectionType.JobRequest:
                    _requestConnection.Settings.SendTimeout = timeout;
                    break;
                case RunnerConnectionType.MessageQueue:
                    _messageConnection.Settings.SendTimeout = timeout;
                    break;
                case RunnerConnectionType.Generic:
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

        private void CheckConnection(RunnerConnectionType connectionType)
        {
            switch (connectionType)
            {
                case RunnerConnectionType.Generic:
                    if (!_hasGenericConnection)
                    {
                        throw new InvalidOperationException($"SetConnection {RunnerConnectionType.Generic}");
                    }
                    break;
                case RunnerConnectionType.JobRequest:
                    if (!_hasRequestConnection)
                    {
                        throw new InvalidOperationException($"SetConnection {RunnerConnectionType.JobRequest}");
                    }
                    break;
                case RunnerConnectionType.MessageQueue:
                    if (!_hasMessageConnection)
                    {
                        throw new InvalidOperationException($"SetConnection {RunnerConnectionType.MessageQueue}");
                    }
                    break;
                default:
                    throw new NotSupportedException(connectionType.ToString());
            }
        }

        //-----------------------------------------------------------------
        // Configuration
        //-----------------------------------------------------------------

        public Task<List<TaskAgentPool>> GetAgentPoolsAsync(string agentPoolName = null, TaskAgentPoolType poolType = TaskAgentPoolType.Automation)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.GetAgentPoolsAsync(agentPoolName, poolType: poolType);
        }

        public Task<TaskAgent> AddAgentAsync(Int32 agentPoolId, TaskAgent agent)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.AddAgentAsync(agentPoolId, agent);
        }

        public Task<List<TaskAgent>> GetAgentsAsync(int agentPoolId, string agentName = null)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.GetAgentsAsync(agentPoolId, agentName, false);
        }

        public Task<TaskAgent> ReplaceAgentAsync(int agentPoolId, TaskAgent agent)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.ReplaceAgentAsync(agentPoolId, agent);
        }

        public Task DeleteAgentAsync(int agentPoolId, int agentId)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.DeleteAgentAsync(agentPoolId, agentId);
        }

        //-----------------------------------------------------------------
        // MessageQueue
        //-----------------------------------------------------------------

        public Task<TaskAgentSession> CreateAgentSessionAsync(Int32 poolId, TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection(RunnerConnectionType.MessageQueue);
            return _messageTaskAgentClient.CreateAgentSessionAsync(poolId, session, cancellationToken: cancellationToken);
        }

        public Task DeleteAgentMessageAsync(Int32 poolId, Int64 messageId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection(RunnerConnectionType.MessageQueue);
            return _messageTaskAgentClient.DeleteMessageAsync(poolId, messageId, sessionId, cancellationToken: cancellationToken);
        }

        public Task DeleteAgentSessionAsync(Int32 poolId, Guid sessionId, CancellationToken cancellationToken)
        {
            CheckConnection(RunnerConnectionType.MessageQueue);
            return _messageTaskAgentClient.DeleteAgentSessionAsync(poolId, sessionId, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentMessage> GetAgentMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            CheckConnection(RunnerConnectionType.MessageQueue);
            return _messageTaskAgentClient.GetMessageAsync(poolId, sessionId, lastMessageId, cancellationToken: cancellationToken);
        }

        //-----------------------------------------------------------------
        // JobRequest
        //-----------------------------------------------------------------

        public Task<TaskAgentJobRequest> RenewAgentRequestAsync(int poolId, long requestId, Guid lockToken, string orchestrationId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckConnection(RunnerConnectionType.JobRequest);
            return _requestTaskAgentClient.RenewAgentRequestAsync(poolId, requestId, lockToken, orchestrationId: orchestrationId, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentJobRequest> FinishAgentRequestAsync(int poolId, long requestId, Guid lockToken, DateTime finishTime, TaskResult result, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckConnection(RunnerConnectionType.JobRequest);
            return _requestTaskAgentClient.FinishAgentRequestAsync(poolId, requestId, lockToken, finishTime, result, cancellationToken: cancellationToken);
        }

        public Task<TaskAgentJobRequest> GetAgentRequestAsync(int poolId, long requestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckConnection(RunnerConnectionType.JobRequest);
            return _requestTaskAgentClient.GetAgentRequestAsync(poolId, requestId, cancellationToken: cancellationToken);
        }

        //-----------------------------------------------------------------
        // Agent Package
        //-----------------------------------------------------------------
        public Task<List<PackageMetadata>> GetPackagesAsync(string packageType, string platform, int top, CancellationToken cancellationToken)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.GetPackagesAsync(packageType, platform, top, cancellationToken: cancellationToken);
        }

        public Task<PackageMetadata> GetPackageAsync(string packageType, string platform, string version, CancellationToken cancellationToken)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.GetPackageAsync(packageType, platform, version, cancellationToken: cancellationToken);
        }

        public Task<TaskAgent> UpdateAgentUpdateStateAsync(int agentPoolId, int agentId, string currentState)
        {
            CheckConnection(RunnerConnectionType.Generic);
            return _genericTaskAgentClient.UpdateAgentUpdateStateAsync(agentPoolId, agentId, currentState);
        }

        //-----------------------------------------------------------------
        // Runner Auth Url
        //-----------------------------------------------------------------
        public Task<string> GetRunnerAuthUrlAsync(int runnerPoolId, int runnerId)
        {
            CheckConnection(RunnerConnectionType.MessageQueue);
            return _messageTaskAgentClient.GetAgentAuthUrlAsync(runnerPoolId, runnerId);
        }

        public Task ReportRunnerAuthUrlErrorAsync(int runnerPoolId, int runnerId, string error)
        {
            CheckConnection(RunnerConnectionType.MessageQueue);
            return _messageTaskAgentClient.ReportAgentAuthUrlMigrationErrorAsync(runnerPoolId, runnerId, error);
        }
    }
}
