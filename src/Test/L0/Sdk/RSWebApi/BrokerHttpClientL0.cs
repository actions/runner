using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.WebApi;
using Xunit;

namespace GitHub.Actions.RunService.WebApi.Tests;

public sealed class BrokerHttpClientL0
{
    [Fact]
    public async Task AcknowledgeRunnerRequestAsyncThrowsRunnerRequestJobNotFoundException()
    {
        using var client = CreateClient(
            HttpStatusCode.NotFound,
            new BrokerError
            {
                Source = "actions-broker-listener",
                ErrorKind = BrokerErrorKind.AcknowledgeJobNotFound,
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = "Assigned job no longer exists",
            });

        await Assert.ThrowsAsync<RunnerRequestJobNotFoundException>(() =>
            client.AcknowledgeRunnerRequestAsync("runner-request", Guid.NewGuid(), "2.0.0", TaskAgentStatus.Online, cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task AcknowledgeRunnerRequestAsyncKeepsRunnerNotFoundClassification()
    {
        using var client = CreateClient(
            HttpStatusCode.NotFound,
            new BrokerError
            {
                Source = "actions-broker-listener",
                ErrorKind = BrokerErrorKind.RunnerNotFound,
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = "Runner not found",
            });

        await Assert.ThrowsAsync<RunnerNotFoundException>(() =>
            client.AcknowledgeRunnerRequestAsync("runner-request", Guid.NewGuid(), "2.0.0", TaskAgentStatus.Online, cancellationToken: CancellationToken.None));
    }

    private static BrokerHttpClient CreateClient(HttpStatusCode statusCode, BrokerError brokerError)
    {
        return new BrokerHttpClient(
            new Uri("https://broker.actions.githubusercontent.com/"),
            new StaticResponseHandler(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonUtility.ToString(brokerError), Encoding.UTF8, "application/json"),
            }),
            disposeHandler: true);
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StaticResponseHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _response.RequestMessage = request;
            return Task.FromResult(_response);
        }
    }
}
