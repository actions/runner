using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using Moq;
using Moq.Protected;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class VssHttpRetryMessageHandlerL0
    {
        [Theory]
        [InlineData(HttpStatusCode.SwitchingProtocols, 0, 0, 0)]
        [InlineData(HttpStatusCode.SwitchingProtocols, 1, 0, 0)]
        [InlineData(HttpStatusCode.OK, 1, 0, 0)]
        [InlineData(HttpStatusCode.NoContent, 0, 0, 0)]
        [InlineData(HttpStatusCode.BadRequest, 1, 1, 0)]
        [InlineData(HttpStatusCode.Forbidden, 1, 1, 0)]
        [InlineData(HttpStatusCode.BadRequest, 0, 0, 1)]
        [InlineData(HttpStatusCode.ServiceUnavailable, 0, 0, 1)]
        public async Task SendAsyncClassifiesResponseWithoutRetrying(
            HttpStatusCode statusCode, int maxRetries, int expectedFailures, int expectedMaxAttemptFailures)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://broker.example/_ws/ping.sock");
            request.Headers.Connection.Add("Upgrade");
            request.Headers.Upgrade.ParseAdd("websocket");
            using var response = new HttpResponseMessage(statusCode)
            {
                RequestMessage = request,
                Content = new StringContent("response"),
            };
            var innerHandler = new Mock<HttpMessageHandler>();
            innerHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", request, ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            using var retryHandler = new TracingRetryHandler(maxRetries, innerHandler.Object);
            using var client = new HttpMessageInvoker(retryHandler);

            var result = await client.SendAsync(request, CancellationToken.None);

            Assert.Same(response, result);
            Assert.Equal("response", await result.Content.ReadAsStringAsync());
            Assert.Equal(expectedFailures, retryHandler.Failures);
            Assert.Equal(expectedMaxAttemptFailures, retryHandler.MaxAttemptFailures);
            innerHandler.Protected().Verify(
                "SendAsync", Times.Once(), request, ItExpr.IsAny<CancellationToken>());
        }

        private sealed class TracingRetryHandler : VssHttpRetryMessageHandler
        {
            public TracingRetryHandler(int maxRetries, HttpMessageHandler innerHandler)
                : base(new VssHttpRetryOptions { MaxRetries = maxRetries }, innerHandler)
            {
            }

            public int Failures { get; private set; }
            public int MaxAttemptFailures { get; private set; }

            protected override void TraceHttpRequestFailed(
                VssTraceActivity activity, HttpRequestMessage request, HttpStatusCode statusCode, string afdRefInfo)
            {
                Failures++;
            }

            protected override void TraceHttpRequestFailedMaxAttempts(
                VssTraceActivity activity, HttpRequestMessage request, int attempt,
                HttpStatusCode? httpStatusCode, WebExceptionStatus? webExceptionStatus,
                SocketError? socketErrorCode, WinHttpErrorCode? winHttpErrorCode,
                CurlErrorCode? curlErrorCode, string afdRefInfo)
            {
                MaxAttemptFailures++;
            }
        }
    }
}
