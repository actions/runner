using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class RunnerDotcomServerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryRequest_SetsRetryHeaderOnRetriesOnly()
        {
            using (TestHostContext hc = CreateTestContext(out var requests, new[] { HttpStatusCode.InternalServerError, HttpStatusCode.OK }))
            {
                var server = new RunnerDotcomServer();
                server.Initialize(hc);

                await server.GetRunnerGroupsAsync("https://github.com/myorg", "token");

                Assert.Equal(new string[] { null, "1" }, requests);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryRequest_NoRetryHeaderWhenFirstAttemptSucceeds()
        {
            using (TestHostContext hc = CreateTestContext(out var requests, new[] { HttpStatusCode.OK }))
            {
                var server = new RunnerDotcomServer();
                server.Initialize(hc);

                await server.GetRunnerGroupsAsync("https://github.com/myorg", "token");

                Assert.Equal(new string[] { null }, requests);
            }
        }

        private TestHostContext CreateTestContext(out List<string> retryHeaders, HttpStatusCode[] statusCodes, [CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            var headers = new List<string>();
            var responses = new Queue<HttpStatusCode>(statusCodes);

            var handlerFactory = new Mock<IHttpClientHandlerFactory>();
            handlerFactory
                .Setup(x => x.CreateClientHandler(It.IsAny<RunnerWebProxy>()))
                .Returns(() => new RecordingHandler(headers, responses));
            hc.SetSingleton<IHttpClientHandlerFactory>(handlerFactory.Object);
            hc.SetSingleton<ITerminal>(new Mock<ITerminal>().Object);

            retryHeaders = headers;
            return hc;
        }

        private sealed class RecordingHandler : HttpClientHandler
        {
            private readonly List<string> _headers;
            private readonly Queue<HttpStatusCode> _responses;

            public RecordingHandler(List<string> headers, Queue<HttpStatusCode> responses)
            {
                _headers = headers;
                _responses = responses;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _headers.Add(request.Headers.TryGetValues("X-Is-Retry", out var values) ? values.Single() : null);
                return Task.FromResult(new HttpResponseMessage(_responses.Dequeue())
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
