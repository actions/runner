using System;
using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;
using System.Net;
using System.Net.Http;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(BrokerServer))]
    public interface IBrokerServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, CancellationToken cancellationToken);
        Task<string> GetMessageAsync(TaskAgentSession session, RunnerSettings settings, long? lastMessageId, CancellationToken cancellationToken);
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

        public async Task<string> GetMessageAsync(TaskAgentSession session, RunnerSettings settings, long? lastMessageId, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"message?tenant=org:github&root_tenant=org:github&group_id={settings.PoolId}&group_name={settings.PoolName}&runner_id={settings.AgentId}&runner_name={settings.AgentName}&labels=self-hosted,linux", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var content = default(string);
                try
                {
                    content = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                }

                var error = $"HTTP {(int)response.StatusCode} {Enum.GetName(typeof(HttpStatusCode), response.StatusCode)}";
                if (!string.IsNullOrEmpty(content))
                {
                    error = $"{error}: {content}";
                }
                throw new Exception(error);
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
