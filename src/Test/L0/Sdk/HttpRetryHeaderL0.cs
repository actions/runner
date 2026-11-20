using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using Xunit;

namespace GitHub.Runner.Common.Tests.Sdk
{
    public sealed class HttpRetryHeaderL0
    {
        private const string IsRetryHeader = "X-Is-Retry";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryHandler_SetsRetryNumberOnRetriesOnly()
        {
            var inner = new RecordingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
            using var invoker = CreateInvoker(inner, maxRetries: 3);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");

            using var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new string[] { null, "1", "2" }, inner.RetryHeaders);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryHandler_NoHeaderWhenFirstAttemptSucceeds()
        {
            var inner = new RecordingHandler(HttpStatusCode.OK);
            using var invoker = CreateInvoker(inner, maxRetries: 3);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");

            using var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(new string[] { null }, inner.RetryHeaders);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryHandler_RemovesCallerSetHeaderOnFirstAttempt()
        {
            var inner = new RecordingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
            using var invoker = CreateInvoker(inner, maxRetries: 3);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
            request.Headers.TryAddWithoutValidation(IsRetryHeader, "7");

            using var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(new string[] { null, "1" }, inner.RetryHeaders);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryHandler_AmbientScopeMarksFirstAttempt()
        {
            var inner = new RecordingHandler(HttpStatusCode.OK, HttpStatusCode.OK);
            using var invoker = CreateInvoker(inner, maxRetries: 3);

            using (HttpRetryScope.Begin(4))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
                using var response = await invoker.SendAsync(request, CancellationToken.None);
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/"))
            using (var response = await invoker.SendAsync(request, CancellationToken.None))
            {
            }

            Assert.Equal(new string[] { "4", null }, inner.RetryHeaders);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RetryScope_RestoresPreviousValue()
        {
            Assert.Equal(0, HttpRetryScope.Current);
            using (HttpRetryScope.Begin(2))
            {
                Assert.Equal(2, HttpRetryScope.Current);
                using (HttpRetryScope.Begin(0))
                {
                    Assert.Equal(0, HttpRetryScope.Current);
                }
                Assert.Equal(2, HttpRetryScope.Current);
            }
            Assert.Equal(0, HttpRetryScope.Current);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RetryScope_DoesNotLeakOutOfAsyncCall()
        {
            async Task RunInScope()
            {
                using (HttpRetryScope.Begin(3))
                {
                    await Task.Yield();
                    Assert.Equal(3, HttpRetryScope.Current);
                }
            }

            await RunInScope();
            Assert.Equal(0, HttpRetryScope.Current);
        }

        private static HttpMessageInvoker CreateInvoker(HttpMessageHandler inner, int maxRetries)
        {
            var options = new VssHttpRetryOptions
            {
                MaxRetries = maxRetries,
                MinBackoff = TimeSpan.FromMilliseconds(1),
                MaxBackoff = TimeSpan.FromMilliseconds(5),
            };
            return new HttpMessageInvoker(new VssHttpRetryMessageHandler(options, inner));
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly Queue<HttpStatusCode> _statusCodes;

            public RecordingHandler(params HttpStatusCode[] statusCodes)
            {
                _statusCodes = new Queue<HttpStatusCode>(statusCodes);
            }

            public List<string> RetryHeaders { get; } = new List<string>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RetryHeaders.Add(request.Headers.TryGetValues(IsRetryHeader, out var values) ? values.Single() : null);
                return Task.FromResult(new HttpResponseMessage(_statusCodes.Dequeue()));
            }
        }
    }
}
