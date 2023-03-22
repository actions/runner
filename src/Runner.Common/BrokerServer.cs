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

        Task<TaskAgentMessage> GetRunnerMessageAsync(CancellationToken token);
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

        public Task<TaskAgentMessage> GetRunnerMessageAsync(CancellationToken cancellationToken)
        {
            CheckConnection();
            var jobMessage = RetryRequest<TaskAgentMessage>(
                async () => await _brokerHttpClient.GetRunnerMessageAsync(cancellationToken), cancellationToken);

            return jobMessage;
        }
    }
}
