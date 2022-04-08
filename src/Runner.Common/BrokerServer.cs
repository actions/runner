using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;
using System.Net.Http;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(BrokerServer))]
    public interface IBrokerServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, CancellationToken cancellationToken);
        Task<GitHub.DistributedTask.WebApi.TaskAgentMessage> GetMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken);
    }

    public sealed class BrokerServer : RunnerService, IBrokerServer
    {
        private HttpClient _httpClient;

        public async Task ConnectAsync(Uri serverUrl, CancellationToken cancellationToken)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = serverUrl;
            _httpClient.Timeout = TimeSpan.FromSeconds(100);
            await _httpClient.GetAsync("health", cancellationToken);
        }

        public async Task<GitHub.DistributedTask.WebApi.TaskAgentMessage> GetMessageAsync(Int32 poolId, Guid sessionId, Int64? lastMessageId, CancellationToken cancellationToken)
        {
            await _httpClient.GetAsync("message", cancellationToken);
            return null;
        }
    }
}
