using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Sdk.RSWebApi.Contracts;
using Sdk.WebApi.WebApi.RawClient;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(BrokerServer))]
    public interface IBrokerServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<TaskAgentSession> CreateSessionAsync(TaskAgentSession session, CancellationToken cancellationToken);
        Task DeleteSessionAsync(CancellationToken cancellationToken);

        Task<TaskAgentMessage> GetRunnerMessageAsync(Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken token);

        Task UpdateConnectionIfNeeded(Uri serverUri, VssCredentials credentials);

        Task ForceRefreshConnection(VssCredentials credentials);
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

        public async Task<TaskAgentSession> CreateSessionAsync(TaskAgentSession session, CancellationToken cancellationToken)
        {
            CheckConnection();
            var jobMessage = await _brokerHttpClient.CreateSessionAsync(session, cancellationToken);

            return jobMessage;
        }

        public Task<TaskAgentMessage> GetRunnerMessageAsync(Guid? sessionId, TaskAgentStatus status, string version, string os, string architecture, bool disableUpdate, CancellationToken cancellationToken)
        {
            CheckConnection();
            var brokerSession = RetryRequest<TaskAgentMessage>(
                async () => await _brokerHttpClient.GetRunnerMessageAsync(sessionId, version, status, os, architecture, disableUpdate, cancellationToken), cancellationToken, shouldRetry: ShouldRetryException);


            return brokerSession;
        }

        public async Task DeleteSessionAsync(CancellationToken cancellationToken)
        {
            CheckConnection();
            await _brokerHttpClient.DeleteSessionAsync(cancellationToken);
        }

        public Task UpdateConnectionIfNeeded(Uri serverUri, VssCredentials credentials)
        {
            if (_brokerUri != serverUri || !_hasConnection)
            {
                return ConnectAsync(serverUri, credentials);
            }

            return Task.CompletedTask;
        }

        public Task ForceRefreshConnection(VssCredentials credentials)
        {
            if (!string.IsNullOrEmpty(_brokerUri?.AbsoluteUri))
            {
                return ConnectAsync(_brokerUri, credentials);
            }

            return Task.CompletedTask;
        }

        public bool ShouldRetryException(Exception ex)
        {
            if (ex is AccessDeniedException || ex is RunnerNotFoundException || ex is HostedRunnerDeprovisionedException)
            {
                return false;
            }

            return true;
        }
    }
}
