using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(RunServer))]
    public interface IRunServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<AgentJobRequestMessage> GetJobMessageAsync(string id);
    }

    public sealed class RunServer : RunnerService, IRunServer
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

        private async Task<VssConnection> EstablishVssConnection(Uri serverUrl, VssCredentials credentials, TimeSpan timeout)
        {
            Trace.Info($"EstablishVssConnection");
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

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException($"SetConnection");
            }
        }

        public Task<AgentJobRequestMessage> GetJobMessageAsync(string id)
        {
            CheckConnection();
            return RequestJobMessageAsync(id);
        }

        private async Task<AgentJobRequestMessage> RequestJobMessageAsync(string id)
        {
            var retryTimeLimit = TimeSpan.FromMinutes(5);
            var remainingTime = retryTimeLimit;
            while (true)
            {
                try
                {
                    return await _taskAgentClient.GetJobMessageAsync(id);
                }
                catch
                {
                    // todo: ask about correct exceptions to handle retriable and non-retrieable cases
                    if(IsRetriableStatusCode(_taskAgentClient.LastResponseContext.HttpStatusCode) && (remainingTime > TimeSpan.Zero))
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                        Trace.Warning($"Back off {backOff.TotalSeconds} seconds before retry.");
                        remainingTime -= backOff;
                        await Task.Delay(backOff);
                    }
                    else
                    {
                        Trace.Info($"Non-retriable exception: {_taskAgentClient.LastResponseContext.Exception.Message}");
                        throw;
                    }
                }
            }
        }

        private bool IsRetriableStatusCode(System.Net.HttpStatusCode statusCode)
        {
            return statusCode == System.Net.HttpStatusCode.BadGateway || 
            statusCode == System.Net.HttpStatusCode.InternalServerError;
        }
    }
}
