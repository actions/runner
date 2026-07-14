using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using Moq;
using Moq.Protected;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class RunnerDotcomServerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetRunnerByNameAsync_RetriesTransientFailureAndSucceeds()
        {
            using (var hc = new TestHostContext(this))
            {
                var mockHandler = new Mock<HttpClientHandler>();
                var expectedUri = new Uri("https://api.github.com/orgs/my-org/actions/runners?name=runner-a");
                mockHandler.Protected()
                    .SetupSequence<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == expectedUri),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("{\"message\":\"server error\"}")
                    })
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"total_count\":1,\"runners\":[{\"id\":7,\"name\":\"runner-a\"}]}")
                    });

                var mockFactory = new Mock<IHttpClientHandlerFactory>();
                mockFactory
                    .Setup(x => x.CreateClientHandler(It.IsAny<RunnerWebProxy>()))
                    .Returns(mockHandler.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(mockFactory.Object);

                var server = new RunnerDotcomServer();
                server.Initialize(hc);

                List<GitHub.DistributedTask.WebApi.TaskAgent> runners = await server.GetRunnerByNameAsync("https://github.com/my-org", "token", "runner-a");

                Assert.Single(runners);
                Assert.Equal(7ul, runners[0].Id);
                Assert.Equal("runner-a", runners[0].Name);

                mockHandler.Protected().Verify(
                    "SendAsync",
                    Times.Exactly(2),
                    ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == expectedUri),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetRunnerByNameAsync_DoesNotRetryNotFound()
        {
            using (var hc = new TestHostContext(this))
            {
                var mockHandler = new Mock<HttpClientHandler>();
                var expectedUri = new Uri("https://api.github.com/orgs/my-org/actions/runners?name=missing-runner");
                mockHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == expectedUri),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{\"message\":\"Not Found\"}")
                    });

                var mockFactory = new Mock<IHttpClientHandlerFactory>();
                mockFactory
                    .Setup(x => x.CreateClientHandler(It.IsAny<RunnerWebProxy>()))
                    .Returns(mockHandler.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(mockFactory.Object);

                var server = new RunnerDotcomServer();
                server.Initialize(hc);

                await Assert.ThrowsAsync<HttpRequestException>(() => server.GetRunnerByNameAsync("https://github.com/my-org", "token", "missing-runner"));

                mockHandler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == expectedUri),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task GetRunnerByNameAsync_StopsAfterMaxAttempts()
        {
            using (var hc = new TestHostContext(this))
            {
                var mockHandler = new Mock<HttpClientHandler>();
                var expectedUri = new Uri("https://api.github.com/orgs/my-org/actions/runners?name=runner-a");
                mockHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == expectedUri),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("{\"message\":\"server error\"}")
                    });

                var mockFactory = new Mock<IHttpClientHandlerFactory>();
                mockFactory
                    .Setup(x => x.CreateClientHandler(It.IsAny<RunnerWebProxy>()))
                    .Returns(mockHandler.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(mockFactory.Object);

                var server = new RunnerDotcomServer();
                server.Initialize(hc);

                await Assert.ThrowsAsync<InvalidOperationException>(() => server.GetRunnerByNameAsync("https://github.com/my-org", "token", "runner-a"));

                mockHandler.Protected().Verify(
                    "SendAsync",
                    Times.Exactly(3),
                    ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == expectedUri),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

    }
}
