using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    // Wire-path tests for the OTLP/HTTP transport using a fake HttpMessageHandler:
    // URL routing, Content-Type, gzip Content-Encoding, gunzip -> original body, auth
    // header, retry behavior, and partial-success — none of which had coverage before.
    public sealed class OTelHttpTransportL0
    {
        private sealed class FakeHandler : HttpMessageHandler
        {
            public readonly List<HttpRequestMessage> Requests = new();
            public readonly List<byte[]> Bodies = new();
            private readonly Queue<Func<HttpResponseMessage>> _responses;

            public FakeHandler(params Func<HttpResponseMessage>[] responses) =>
                _responses = new Queue<Func<HttpResponseMessage>>(responses);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Requests.Add(request);
                Bodies.Add(request.Content != null ? await request.Content.ReadAsByteArrayAsync(ct) : Array.Empty<byte>());
                return _responses.Count > 0 ? _responses.Dequeue()() : new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private static HttpResponseMessage Resp(HttpStatusCode code, string body = "") =>
            new(code) { Content = new StringContent(body) };

        private static string Gunzip(byte[] b)
        {
            using var ms = new MemoryStream(b);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var sr = new StreamReader(gz);
            return sr.ReadToEnd();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_RoutesGzipsAndAttachesAuthHeader()
        {
            var handler = new FakeHandler(() => Resp(HttpStatusCode.OK));
            using var transport = new OTelHttpTransport(handler,
                new[] { new KeyValuePair<string, string>("authorization", "Bearer xyz") });

            var ok = await transport.PostAsync("http://collector/v1/traces", "{\"resourceSpans\":[]}", "spans", default);

            Assert.True(ok);
            var req = handler.Requests.Single();
            Assert.Equal("http://collector/v1/traces", req.RequestUri.ToString());
            Assert.Equal("application/json", req.Content.Headers.ContentType.MediaType);
            Assert.Contains("gzip", req.Content.Headers.ContentEncoding);
            Assert.Equal("Bearer xyz", req.Headers.GetValues("authorization").Single());
            Assert.Equal("{\"resourceSpans\":[]}", Gunzip(handler.Bodies[0])); // gunzips to the original OTLP
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_SendsIdentifyingUserAgent()
        {
            // OTLP exporter spec SHOULD: a User-Agent identifying the exporter + version,
            // so collector operators can attribute runner-fleet traffic.
            var handler = new FakeHandler(() => Resp(HttpStatusCode.OK));
            using var transport = new OTelHttpTransport(handler, userAgent: "GitHubActionsRunner-OTLP-Exporter/2.333.0");

            await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.Equal("GitHubActionsRunner-OTLP-Exporter/2.333.0",
                string.Join(" ", handler.Requests.Single().Headers.GetValues("User-Agent")));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_RetriesOnTransientThenSucceeds()
        {
            var handler = new FakeHandler(() => Resp(HttpStatusCode.ServiceUnavailable), () => Resp(HttpStatusCode.OK));
            using var transport = new OTelHttpTransport(handler);

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.True(ok);
            Assert.Equal(2, handler.Requests.Count); // 503 then retried -> 200
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_DoesNotRetryOnClientError()
        {
            var handler = new FakeHandler(() => Resp(HttpStatusCode.BadRequest));
            using var transport = new OTelHttpTransport(handler);

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.False(ok);
            Assert.Single(handler.Requests); // 4xx is not retried
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(HttpStatusCode.RequestTimeout)]       // 408
        [InlineData(HttpStatusCode.InternalServerError)]  // 500
        public async Task Post_DoesNotRetryNonRetryableStatuses(HttpStatusCode code)
        {
            // OTLP/HTTP failures table: only 429/502/503/504 are retryable;
            // all other 4xx/5xx MUST NOT be retried.
            var handler = new FakeHandler(() => Resp(code));
            using var transport = new OTelHttpTransport(handler);

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.False(ok);
            Assert.Single(handler.Requests);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_HonorsRetryAfterOnThrottle()
        {
            // OTLP spec SHOULD: wait as long as the server's Retry-After before retrying.
            var handler = new FakeHandler(
                () =>
                {
                    var r = Resp(HttpStatusCode.TooManyRequests);
                    r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
                    return r;
                },
                () => Resp(HttpStatusCode.OK));
            using var transport = new OTelHttpTransport(handler);
            var delays = new List<TimeSpan>();
            transport.DelayAsync = (d, _) => { delays.Add(d); return Task.CompletedTask; };

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.True(ok);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(TimeSpan.FromSeconds(1), delays.Single());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_SkipsRetryWhenRetryAfterExceedsFlushDeadline()
        {
            // A Retry-After the 4 s flush deadline can't honor: don't burn the retry.
            var handler = new FakeHandler(() =>
            {
                var r = Resp(HttpStatusCode.TooManyRequests);
                r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
                return r;
            });
            using var transport = new OTelHttpTransport(handler);
            var delays = new List<TimeSpan>();
            transport.DelayAsync = (d, _) => { delays.Add(d); return Task.CompletedTask; };

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.False(ok);
            Assert.Single(handler.Requests);
            Assert.Empty(delays);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_JittersRetryDelayWithoutRetryAfter()
        {
            // No Retry-After -> short jittered delay (100-400 ms), not a fixed interval,
            // so a fleet of runners flushing at once doesn't retry in lockstep.
            var handler = new FakeHandler(() => Resp(HttpStatusCode.ServiceUnavailable), () => Resp(HttpStatusCode.OK));
            using var transport = new OTelHttpTransport(handler);
            var delays = new List<TimeSpan>();
            transport.DelayAsync = (d, _) => { delays.Add(d); return Task.CompletedTask; };

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.True(ok);
            Assert.Equal(2, handler.Requests.Count);
            Assert.InRange(delays.Single().TotalMilliseconds, 100, 400);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_PartialSuccessIsDeliveredAndLogged()
        {
            var handler = new FakeHandler(() => Resp(HttpStatusCode.OK, "{\"partialSuccess\":{\"rejectedSpans\":\"2\",\"errorMessage\":\"dropped\"}}"));
            string logged = null;
            using var transport = new OTelHttpTransport(handler, null, m => logged = m);

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", default);

            Assert.True(ok);
            Assert.Contains("partially rejected", logged);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task Post_ExpiredDeadlineDoesNotThrowOrRetryForever()
        {
            var handler = new FakeHandler(() => Resp(HttpStatusCode.ServiceUnavailable));
            using var transport = new OTelHttpTransport(handler);
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // deadline already blown

            var ok = await transport.PostAsync("http://collector/v1/traces", "{}", "spans", cts.Token);

            Assert.False(ok); // best-effort: swallowed, never throws
        }
    }
}
