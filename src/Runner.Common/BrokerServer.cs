#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using Sdk.WebApi.WebApi.RawClient;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(BrokerServer))]
    public interface IBrokerServer : IRunnerService
    {
        Task<BrokerSession> CreateSessionAsync(Uri serverUrl, VssCredentials credentials, CancellationToken token);

        Task<TaskAgentMessage> GetRunnerMessageAsync(CancellationToken token, TaskAgentStatus status, string version);
    }

    public sealed class BrokerServer : RunnerService, IBrokerServer
    {
        private RawConnection? _connection;
        private BrokerHttpClient? _brokerHttpClient;
        private BrokerSession? _session;

        public async Task<BrokerSession> CreateSessionAsync(Uri serverUri, VssCredentials credentials, CancellationToken cancellationToken)
        {
            _connection = VssUtil.CreateRawConnection(serverUri, credentials);
            _brokerHttpClient = await _connection.GetClientAsync<BrokerHttpClient>(cancellationToken);
            return await RetryRequest(
                async () => _session = await _brokerHttpClient.CreateSessionAsync(),
                cancellationToken
            );
        }

        public Task<TaskAgentMessage> GetRunnerMessageAsync(CancellationToken cancellationToken, TaskAgentStatus status, string version)
        {
            if (_connection is null || _session is null || _brokerHttpClient is null)
            {
                throw new InvalidOperationException($"SetConnection");
            }
            var jobMessage = RetryRequest<TaskAgentMessage>(
                async () => await _brokerHttpClient.GetRunnerMessageAsync(_session.id, version, status, cancellationToken), cancellationToken);

            return jobMessage;
        }
    }
}
