using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(ActionsRunServer))]
    public interface IActionsRunServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<AgentJobRequestMessage> GetJobMessageAsync(string id, CancellationToken token);
    }

    public sealed class ActionsRunServer : RunnerService, IActionsRunServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskAgentHttpClient _taskAgentClient;

        public async Task ConnectAsync(Uri serverUrl, VssCredentials credentials)
        {
            _connection = await EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(100));
            _taskAgentClient = _connection.GetClient<TaskAgentHttpClient>();
            _hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException($"SetConnection");
            }
        }

        public Task<AgentJobRequestMessage> GetJobMessageAsync(string id, CancellationToken cancellationToken)
        {
            CheckConnection();
            var jobMessage = RetryRequest<AgentJobRequestMessage>(async () =>
                                                    {
                                                        return await _taskAgentClient.GetJobMessageAsync(id, cancellationToken);
                                                    }, cancellationToken);

            return jobMessage;
        }
    }
}
