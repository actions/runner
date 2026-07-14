using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
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

        Task<AgentJobRequestMessage> GetJobMessageAsync(string id, string billingOwnerId, CancellationToken token);

        Task CompleteJobAsync(
            Guid planId,
            Guid jobId,
            TaskResult result,
            Dictionary<String, VariableValue> outputs,
            IList<StepResult> stepResults,
            IList<Annotation> jobAnnotations,
            string environmentUrl,
            IList<Telemetry> telemetry,
            string billingOwnerId,
            string infrastructureFailureCategory,
            CancellationToken token);

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

        public Task<AgentJobRequestMessage> GetJobMessageAsync(string id, string billingOwnerId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return RetryRequest<AgentJobRequestMessage>(
                async () =>
                {
                    try
                    {
                        return OperationOutcome.Success(
                            await _runServiceHttpClient.GetJobMessageAsync(requestUri, id, VarUtil.OS, billingOwnerId, cancellationToken));
                    }
                    catch (TaskOrchestrationJobNotFoundException) { throw; }    // HTTP status 404
                    catch (TaskOrchestrationJobAlreadyAcquiredException) { throw; } // HTTP status 409
                    catch (TaskOrchestrationJobUnprocessableException) { throw; }   // HTTP status 422
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { return OperationOutcome.TransientFailure<AgentJobRequestMessage>(ex.Message); }
                }, cancellationToken);
        }

        public Task CompleteJobAsync(
            Guid planId,
            Guid jobId,
            TaskResult result,
            Dictionary<String, VariableValue> outputs,
            IList<StepResult> stepResults,
            IList<Annotation> jobAnnotations,
            string environmentUrl,
            IList<Telemetry> telemetry,
            string billingOwnerId,
            string infrastructureFailureCategory,
            CancellationToken cancellationToken)
        {
            CheckConnection();
            return RetryRequest(
                async () =>
                {
                    try
                    {
                        await _runServiceHttpClient.CompleteJobAsync(requestUri, planId, jobId, result, outputs, stepResults, jobAnnotations, environmentUrl, telemetry, billingOwnerId, infrastructureFailureCategory, cancellationToken);
                        return OperationOutcome.Success(true);
                    }
                    catch (VssUnauthorizedException) { throw; }             // HTTP status 401
                    catch (TaskOrchestrationJobNotFoundException) { throw; } // HTTP status 404
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { return OperationOutcome.TransientFailure<bool>(ex.Message); }
                }, cancellationToken);
        }

        public Task<RenewJobResponse> RenewJobAsync(Guid planId, Guid jobId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return RetryRequest<RenewJobResponse>(
                async () =>
                {
                    try
                    {
                        return OperationOutcome.Success(
                            await _runServiceHttpClient.RenewJobAsync(requestUri, planId, jobId, cancellationToken));
                    }
                    catch (TaskOrchestrationJobNotFoundException) { throw; } // HTTP status 404
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { return OperationOutcome.TransientFailure<RenewJobResponse>(ex.Message); }
                }, cancellationToken);
        }
    }
}
