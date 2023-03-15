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
    [ServiceLocator(Default = typeof(RunServer))]
    public interface IRunServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<AgentJobRequestMessage> GetJobMessageAsync(string id, CancellationToken token);

        Task CompleteJobAsync(Guid planId, Guid jobId, TaskResult result, Dictionary<String, VariableValue> outputs, IList<StepResult> stepResults, CancellationToken token);

        Task<RenewJobResponse> RenewJobAsync(Guid planId, Guid jobId, CancellationToken token);
    }

    public sealed class RunServer : RunnerService, IRunServer
    {
        private bool _hasConnection;
        private Uri requestUri;
        private RawConnection _connection;
        private RunServiceHttpClient _runServiceHttpClient;

        public async Task ConnectAsync(Uri serverUri, VssCredentials credentials)
        {
            requestUri = serverUri;

            _connection = VssUtil.CreateRawConnection(serverUri, credentials);
            _runServiceHttpClient = await _connection.GetClientAsync<RunServiceHttpClient>();
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
            var jobMessage = RetryRequest<AgentJobRequestMessage>(
                async () => await _runServiceHttpClient.GetJobMessageAsync(requestUri, id, cancellationToken), cancellationToken);
            if (jobMessage == null)
            {
                throw new TaskOrchestrationJobNotFoundException(id);
            }

            return jobMessage;
        }

        public Task CompleteJobAsync(Guid planId, Guid jobId, TaskResult result, Dictionary<String, VariableValue> outputs, IList<StepResult> stepResults, CancellationToken cancellationToken)
        {
            CheckConnection();
            return RetryRequest(
                async () => await _runServiceHttpClient.CompleteJobAsync(requestUri, planId, jobId, result, outputs, stepResults, cancellationToken), cancellationToken);
        }

        public Task<RenewJobResponse> RenewJobAsync(Guid planId, Guid jobId, CancellationToken cancellationToken)
        {
            CheckConnection();
            var renewJobResponse = RetryRequest<RenewJobResponse>(
                async () => await _runServiceHttpClient.RenewJobAsync(requestUri, planId, jobId, cancellationToken), cancellationToken);
            if (renewJobResponse == null)
            {
                throw new TaskOrchestrationJobNotFoundException(jobId.ToString());
            }

            return renewJobResponse;
        }
    }
}
