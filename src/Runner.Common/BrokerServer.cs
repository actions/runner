using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using Sdk.RSWebApi.Contracts;
using Sdk.WebApi.WebApi.RawClient;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(BrokerServer))]
    public interface IBrokerServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<TaskAgentSession> CreateSessionAsync(TaskAgentSession session, CancellationToken cancellationToken);

        Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken);
        Task<TaskAgentMessage> GetRunnerMessageAsync(Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken token);
    }

    public sealed class BrokerServer : RunnerService, IBrokerServer
    {
        private bool _hasConnection;
        private Uri _brokerUri;
        private RawConnection _connection;
        private BrokerHttpClient _brokerHttpClient;

        public async Task ConnectAsync(Uri serverUri, VssCredentials credentials)
        {
            _brokerUri = serverUri;

            _connection = VssUtil.CreateRawConnection(serverUri, credentials);
            _brokerHttpClient = await _connection.GetClientAsync<BrokerHttpClient>();
            _hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException($"SetConnection");
            }
        }

        public Task<TaskAgentSession> CreateSessionAsync(TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection();
            var jobMessage = RetryRequest<TaskAgentSession>(
                async () => await _brokerHttpClient.CreateSessionAsync(session, cancellationToken), cancellationToken);

            return jobMessage;
        }

        public Task<TaskAgentMessage> GetRunnerMessageAsync(Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken)
        {
            CheckConnection();
            var brokerSession = RetryRequest<TaskAgentMessage>(
                async () => await _brokerHttpClient.GetRunnerMessageAsync(sessionId, version, status, os, architecture, disableUpdate, cancellationToken), cancellationToken);

            return brokerSession;
        }

        public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // not implemented yet
        }
    }
}
