using System;
using System.Collections.Generic;
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
        Task<string> GetMessageAsync(int poolId, Guid sessionId, long? lastMessageId, CancellationToken cancellationToken);
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

        public async Task<string> GetMessageAsync(int poolId, Guid sessionId, long? lastMessageId, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync("message", cancellationToken);
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
