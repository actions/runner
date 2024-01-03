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
        Task<BrokerSession> ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<TaskAgentMessage> GetRunnerMessageAsync(CancellationToken token, TaskAgentStatus status, string version);
    }

    public sealed class BrokerServer : RunnerService, IBrokerServer
    {
        private bool _hasConnection;
        private Uri _brokerUri;
        private RawConnection _connection;
        private BrokerHttpClient _brokerHttpClient;
        private bool _hasSession;
        private BrokerSession _session;

        public async Task<BrokerSession> ConnectAsync(Uri serverUri, VssCredentials credentials)
        {
            _brokerUri = serverUri;

            _connection = VssUtil.CreateRawConnection(serverUri, credentials);
            _brokerHttpClient = await _connection.GetClientAsync<BrokerHttpClient>();
            _session = await _brokerHttpClient.CreateSessionAsync();
            _hasConnection = true;
            _hasSession = true;

            return _session;
        }

        private void CheckConnection()
        {
            if (!_hasConnection || !_hasSession)
            {
                throw new InvalidOperationException($"SetConnection");
            }
        }

        public Task<TaskAgentMessage> GetRunnerMessageAsync(CancellationToken cancellationToken, TaskAgentStatus status, string version)
        {
            CheckConnection();
            var jobMessage = RetryRequest<TaskAgentMessage>(
                async () => await _brokerHttpClient.GetRunnerMessageAsync(_session.id, version, status, cancellationToken), cancellationToken);

            return jobMessage;
        }
    }
}
