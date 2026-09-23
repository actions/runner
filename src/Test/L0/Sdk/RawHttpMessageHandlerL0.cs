using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.Services.Common;
using GitHub.Services.Common.Diagnostics;
using Sdk.WebApi.WebApi;
using Sdk.WebApi.WebApi.RawClient;
using Xunit;

namespace GitHub.Runner.Common.Tests;

public sealed class RawHttpMessageHandlerL0
{
    private static readonly TimeSpan s_hangWatchdog = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task AcquireJobTimesOutWhenResponseBodyStalls()
    {
        await using var server = new ResponseServer();
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using RawConnection connection = CreateConnectionLikeRunServer(server.Address, handler);
        RunServiceHttpClient client = await connection.GetClientAsync<RunServiceHttpClient>();
        using var session = new CancellationTokenSource();

        Task<AgentJobRequestMessage> acquireJob = client.GetJobMessageAsync(server.Address, "job-id", "Linux", "owner", session.Token);
        await server.ResponseStarted.WaitAsync(s_hangWatchdog);
        Assert.StartsWith("POST /acquirejob HTTP/1.1\r\n", server.RequestHeaders);

        TimeoutException exception = await AssertThrowsWithoutHangingAsync<TimeoutException>(() => acquireJob);
        Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
        Assert.False(session.IsCancellationRequested);
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task NonRetryableStatusWithStalledBodyTimesOut()
    {
        byte[] conflictWithStalledErrorBody = CreateResponse("Content-Length: 2\r\n", "{", "409 Conflict");
        await using var server = new ResponseServer(response: conflictWithStalledErrorBody);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using RawConnection connection = CreateConnectionLikeRunServer(server.Address, handler);
        RunServiceHttpClient client = await connection.GetClientAsync<RunServiceHttpClient>();

        Task<AgentJobRequestMessage> acquireJob = client.GetJobMessageAsync(server.Address, "job-id", "Linux", "owner", CancellationToken.None);
        await AssertThrowsWithoutHangingAsync<TimeoutException>(() => acquireJob);
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task RetryableStatusWithStalledBodyIsRetried()
    {
        var stalledReply = new Reply(CreateResponse("Content-Length: 2\r\n", "{", "503 Service Unavailable"), stallBody: true);
        var successfulRetry = new Reply(CreateResponse("Content-Length: 2\r\n", "{}"));
        await using var server = new SequenceServer(stalledReply, successfulRetry);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using HttpMessageHandler pipeline = CreateRetryPipeline(transport, maxRetries: 1);
        using var client = new TestClient(server.Address, pipeline);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, server.RequestCount);
        await stalledReply.Closed.Task.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task RetryableStatusWithTruncatedBodyIsRetried()
    {
        var truncatedReply = new Reply(CreateResponse("Content-Length: 10\r\n", "{", "503 Service Unavailable"));
        var successfulRetry = new Reply(CreateResponse("Content-Length: 2\r\n", "{}"));
        await using var server = new SequenceServer(truncatedReply, successfulRetry);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using HttpMessageHandler pipeline = CreateRetryPipeline(transport, maxRetries: 1);
        using var client = new TestClient(server.Address, pipeline);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, server.RequestCount);
    }

    [Fact]
    public async Task StalledBodyOnLastRetryAttemptTimesOut()
    {
        const int MaxRetries = 1;
        var retriedReply = new Reply(CreateResponse("Content-Length: 2\r\n", "{}", "503 Service Unavailable"));
        var stalledReplyOnLastAttempt = new Reply(CreateResponse("Content-Length: 2\r\n", "{", "503 Service Unavailable"), stallBody: true);
        await using var server = new SequenceServer(retriedReply, stalledReplyOnLastAttempt);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using HttpMessageHandler pipeline = CreateRetryPipeline(transport, MaxRetries);
        using var client = new TestClient(server.Address, pipeline);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        await AssertThrowsWithoutHangingAsync<TimeoutException>(() => client.SendAsync(request));
        await stalledReplyOnLastAttempt.Closed.Task.WaitAsync(s_hangWatchdog);
        Assert.Equal(MaxRetries + 1, server.RequestCount);
    }

    [Fact]
    public async Task ConnectionResetDuringBodyIsNotRetried()
    {
        using var transport = new HeadersReceivedHandler(new SocketsHttpHandler { UseProxy = false });
        var resetDuringBody = new Reply(CreateResponse("Content-Length: 10\r\n", "{}"), resetAfter: transport.HeadersReceived);
        var successfulRetry = new Reply(CreateResponse("Content-Length: 2\r\n", "{}"));
        await using var server = new SequenceServer(resetDuringBody, successfulRetry);
        using HttpMessageHandler pipeline = CreateRetryPipeline(transport, maxRetries: 1);
        using var client = new TestClient(server.Address, pipeline);
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Address) { Content = new StringContent("{}") };

        await AssertThrowsWithoutHangingAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(1, server.RequestCount);
    }

    [Fact]
    public async Task TruncatedBodyKeepsResponseEndedError()
    {
        await using var server = new SequenceServer(new Reply(CreateResponse("Content-Length: 10\r\n", "{}")));
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        HttpRequestException exception = await AssertThrowsWithoutHangingAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(HttpRequestError.ResponseEnded, exception.HttpRequestError);
    }

    [Fact]
    public async Task SlowBodyThatKeepsArrivingIsNotTimedOut()
    {
        RawClientHttpRequestSettings settings = CreateSettingsFromRunnerDefaults();
        int chunksSpanningTwiceTheTimeout = ChunksSpanning(settings.SendTimeout * 2);
        await using var server = new ResponseServer(
            response: CreateResponse("Transfer-Encoding: chunked\r\n", "2\r\n{}\r\n"), dripChunks: chunksSpanningTwiceTheTimeout, finishBody: true);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), settings, transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        var stopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.True(stopwatch.Elapsed > settings.SendTimeout);
        Assert.Equal("{}" + new string(' ', chunksSpanningTwiceTheTimeout), await response.Content.ReadAsStringAsync());
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task BodyThatNeverStartsTimesOut()
    {
        byte[] headersWithoutBody = CreateResponse("Content-Length: 2\r\n", "");
        await using var server = new ResponseServer(response: headersWithoutBody);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        RawClientHttpRequestSettings settings = CreateSettingsFromRunnerDefaults();
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), settings, transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        var stopwatch = Stopwatch.StartNew();
        await AssertThrowsWithoutHangingAsync<TimeoutException>(() => client.SendAsync(request));
        Assert.True(stopwatch.Elapsed >= settings.SendTimeout);
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task BodyThatStopsArrivingTimesOut()
    {
        RawClientHttpRequestSettings settings = CreateSettingsFromRunnerDefaults();
        int chunksSpanningTwiceTheTimeout = ChunksSpanning(settings.SendTimeout * 2);
        await using var server = new ResponseServer(
            response: CreateResponse("Transfer-Encoding: chunked\r\n", "2\r\n{}\r\n"), dripChunks: chunksSpanningTwiceTheTimeout);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), settings, transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        await AssertThrowsWithoutHangingAsync<TimeoutException>(() => client.SendAsync(request));
        int chunksSentBeforeTimeout = server.ChunksSent;
        Assert.Equal(chunksSpanningTwiceTheTimeout, chunksSentBeforeTimeout);
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task CallerCancellationDuringBodyReadRemainsCancellation()
    {
        await using var server = new ResponseServer();
        using var transport = new HeadersReceivedHandler(new SocketsHttpHandler { UseProxy = false });
        RawClientHttpRequestSettings settings = CreateSettingsFromRunnerDefaults();
        settings.SendTimeout = TimeSpan.FromMinutes(1);
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), settings, transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);
        using var cancellation = new CancellationTokenSource();

        TimeSpan wellBeforeSendTimeout = TimeSpan.FromSeconds(2);

        Task<HttpResponseMessage> send = client.SendAsync(request, cancellationToken: cancellation.Token);
        await transport.HeadersReceived.WaitAsync(s_hangWatchdog);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send).WaitAsync(wellBeforeSendTimeout);
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task ResponseHeadersReadDoesNotWaitForBody()
    {
        await using var server = new ResponseServer();
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(s_hangWatchdog))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(server.ConnectionClosed.IsCompleted);
        }

        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task ResponseHeadersReadBodyCanBeReadAfterTimeout()
    {
        await using var server = new ResponseServer(
            response: CreateResponse("Content-Length: 2\r\n", ""), delayedBody: Encoding.ASCII.GetBytes("{}"));
        using var transport = new SocketsHttpHandler { UseProxy = false };
        RawClientHttpRequestSettings settings = CreateSettingsFromRunnerDefaults();
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), settings, transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(s_hangWatchdog);
        await Task.Delay(settings.SendTimeout * 2);
        server.ReleaseBody.TrySetResult(true);

        Assert.Equal("{}", await response.Content.ReadAsStringAsync().WaitAsync(s_hangWatchdog));
    }

    [Fact]
    public async Task NormalBodyRemainsReadable()
    {
        var body = new string('x', 1024 * 1024);
        await using var server = new ResponseServer(response: CreateResponse($"Content-Length: {body.Length}\r\n", body));
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HeadResponseWithContentLengthDoesNotWaitForBody()
    {
        await using var server = new ResponseServer();
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Head, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(2L, response.Content.Headers.ContentLength);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.NoContent, "204 No Content", "")]
    [InlineData(HttpStatusCode.OK, "200 OK", "Content-Length: 0\r\n")]
    public async Task EmptyResponsesRemainReadable(HttpStatusCode expectedStatus, string status, string headers)
    {
        await using var server = new ResponseServer(response: CreateResponse(headers, "", status));
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BufferedResponseKeepsContentHeaders()
    {
        byte[] latin1Response = Encoding.Latin1.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=iso-8859-1\r\nContent-Language: de\r\nContent-Length: 1\r\nConnection: close\r\n\r\né");
        await using var server = new ResponseServer(response: latin1Response);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal("iso-8859-1", response.Content.Headers.ContentType.CharSet);
        Assert.Equal(new[] { "de" }, response.Content.Headers.ContentLanguage);
        Assert.Equal(1L, response.Content.Headers.ContentLength);
        Assert.Equal("é", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HeadersThatNeverArriveTimeOut()
    {
        var noResponse = new Reply(Array.Empty<byte>(), stallBody: true);
        await using var server = new SequenceServer(noResponse);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        await AssertThrowsWithoutHangingAsync<TimeoutException>(() => client.SendAsync(request));
        await noResponse.Closed.Task.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task DisabledSendTimeoutLeavesStalledBodyToTheCaller()
    {
        await using var server = new ResponseServer();
        using var transport = new SocketsHttpHandler { UseProxy = false };
        RawClientHttpRequestSettings settings = CreateSettingsFromRunnerDefaults();
        TimeSpan longerThanTheUsualTimeout = settings.SendTimeout * 2;
        settings.SendTimeout = TimeSpan.Zero;
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), settings, transport);
        using var client = new TestClient(server.Address, handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);
        using var cancellation = new CancellationTokenSource();

        Task<HttpResponseMessage> send = client.SendAsync(request, cancellationToken: cancellation.Token);
        await server.ResponseStarted.WaitAsync(s_hangWatchdog);
        await Task.Delay(longerThanTheUsualTimeout);
        Assert.False(send.IsCompleted);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send).WaitAsync(s_hangWatchdog);
        await server.ConnectionClosed.WaitAsync(s_hangWatchdog);
    }

    [Fact]
    public async Task AcquireJobReturnsParsedJobMessage()
    {
        var jobId = Guid.NewGuid();
        string body = $"{{\"jobId\":\"{jobId}\",\"jobDisplayName\":\"build\"}}";
        await using var server = new ResponseServer(response: CreateResponse($"Content-Length: {body.Length}\r\n", body));
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using RawConnection connection = CreateConnectionLikeRunServer(server.Address, handler);
        RunServiceHttpClient client = await connection.GetClientAsync<RunServiceHttpClient>();

        AgentJobRequestMessage message = await client.GetJobMessageAsync(server.Address, "job-id", "Linux", "owner", CancellationToken.None).WaitAsync(s_hangWatchdog);
        Assert.Equal(jobId, message.JobId);
        Assert.Equal("build", message.JobDisplayName);
    }

    [Fact]
    public async Task AcquireJobConflictIsReportedAsAlreadyAcquired()
    {
        const string ErrorBody = "{\"source\":\"actions-run-service\",\"statusCode\":409,\"errorMessage\":\"taken\"}";
        await using var server = new ResponseServer(response: CreateResponse($"Content-Length: {ErrorBody.Length}\r\n", ErrorBody, "409 Conflict"));
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        using RawConnection connection = CreateConnectionLikeRunServer(server.Address, handler);
        RunServiceHttpClient client = await connection.GetClientAsync<RunServiceHttpClient>();

        await AssertThrowsWithoutHangingAsync<GitHub.DistributedTask.WebApi.TaskOrchestrationJobAlreadyAcquiredException>(
            () => client.GetJobMessageAsync(server.Address, "job-id", "Linux", "owner", CancellationToken.None));
    }

    [Fact]
    public async Task RetryableStatusReturnsLastResponseWhenRetriesRunOut()
    {
        var firstReply = new Reply(CreateResponse("Content-Length: 2\r\n", "{}", "503 Service Unavailable"));
        var lastReply = new Reply(CreateResponse("Content-Length: 2\r\n", "{}", "503 Service Unavailable"));
        await using var server = new SequenceServer(firstReply, lastReply);
        using var transport = new SocketsHttpHandler { UseProxy = false };
        using HttpMessageHandler pipeline = CreateRetryPipeline(transport, maxRetries: 1);
        using var client = new TestClient(server.Address, pipeline);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("{}", await response.Content.ReadAsStringAsync());
        Assert.Equal(2, server.RequestCount);
    }

    [Fact]
    public async Task RetryableStatusWithResetBodyIsRetriedFromStatus()
    {
        using var transport = new HeadersReceivedHandler(new SocketsHttpHandler { UseProxy = false });
        var resetDuringBody = new Reply(CreateResponse("Content-Length: 10\r\n", "{", "503 Service Unavailable"), resetAfter: transport.HeadersReceived);
        var successfulRetry = new Reply(CreateResponse("Content-Length: 2\r\n", "{}"));
        await using var server = new SequenceServer(resetDuringBody, successfulRetry);
        using RecordingRetryHandler pipeline = CreateRetryPipeline(transport, maxRetries: 1);
        using var client = new TestClient(server.Address, pipeline);
        using var request = new HttpRequestMessage(HttpMethod.Get, server.Address);

        using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(s_hangWatchdog);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new HttpStatusCode?[] { HttpStatusCode.ServiceUnavailable }, pipeline.RetriedStatusCodes);
    }

    private static RawClientHttpRequestSettings CreateSettingsFromRunnerDefaults()
    {
        RawClientHttpRequestSettings settings = RawClientHttpRequestSettings.Default.Clone();
        settings.SendTimeout = TimeSpan.FromSeconds(1);
        return settings;
    }

    private static Task<TException> AssertThrowsWithoutHangingAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        return Assert.ThrowsAsync<TException>(action).WaitAsync(s_hangWatchdog);
    }

    private static RawConnection CreateConnectionLikeRunServer(Uri address, RawHttpMessageHandler handler)
    {
        return new RawConnection(address, handler, null);
    }

    private static int ChunksSpanning(TimeSpan duration)
    {
        return (int)(duration / ResponseServer.DripInterval);
    }

    private static RecordingRetryHandler CreateRetryPipeline(HttpMessageHandler transport, int maxRetries)
    {
        var retryOptions = new VssHttpRetryOptions
        {
            MaxRetries = maxRetries,
            MinBackoff = TimeSpan.FromMilliseconds(10),
            MaxBackoff = TimeSpan.FromMilliseconds(10),
        };
        var handler = new RawHttpMessageHandler(new NoOpCredentials(null), CreateSettingsFromRunnerDefaults(), transport);
        return new RecordingRetryHandler(retryOptions, handler);
    }

    private static byte[] CreateResponse(string headers, string body, string status = "200 OK")
    {
        return Encoding.Latin1.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nConnection: close\r\n{headers}\r\n{body}");
    }

    private static async Task<string> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var headers = new StringBuilder();
        var buffer = new byte[1];
        while (!headers.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
        {
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            headers.Append((char)buffer[0]);
        }

        string requestHeaders = headers.ToString();
        foreach (string header in requestHeaders.Split("\r\n"))
        {
            const string ContentLengthHeader = "Content-Length:";
            if (header.StartsWith(ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                var requestBody = new byte[int.Parse(header.Substring(ContentLengthHeader.Length).Trim())];
                await stream.ReadExactlyAsync(requestBody, cancellationToken).ConfigureAwait(false);
            }
        }

        return requestHeaders;
    }

    private static async Task AssertClosedByPeerAsync(Task<int> disconnected)
    {
        try
        {
            Assert.Equal(0, await disconnected.ConfigureAwait(false));
        }
        catch (IOException ex) when (IsConnectionReset(ex))
        {
        }
    }

    private static bool IsConnectionReset(IOException ex)
    {
        return ex.InnerException is SocketException socketException &&
            (socketException.SocketErrorCode == SocketError.ConnectionReset || socketException.SocketErrorCode == SocketError.ConnectionAborted);
    }

    private static void ResetConnection(TcpClient connection)
    {
        connection.Client.LingerState = new LingerOption(true, 0);
        connection.Client.Close();
    }

    private sealed class TestClient : RawHttpClientBase
    {
        public TestClient(Uri address, HttpMessageHandler handler)
            : base(address, handler, disposeHandler: false)
        {
        }

        public Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
            CancellationToken cancellationToken = default)
        {
            return base.SendAsync(request, completionOption, cancellationToken: cancellationToken);
        }
    }

    private sealed class RecordingRetryHandler : VssHttpRetryMessageHandler
    {
        public RecordingRetryHandler(VssHttpRetryOptions options, HttpMessageHandler innerHandler)
            : base(options, innerHandler)
        {
        }

        public List<HttpStatusCode?> RetriedStatusCodes { get; } = new List<HttpStatusCode?>();

        protected override void TraceHttpRequestRetrying(
            VssTraceActivity activity,
            HttpRequestMessage request,
            int attempt,
            TimeSpan backoffDuration,
            HttpStatusCode? httpStatusCode,
            WebExceptionStatus? webExceptionStatus,
            SocketError? socketErrorCode,
            WinHttpErrorCode? winHttpErrorCode,
            CurlErrorCode? curlErrorCode,
            string afdRefInfo)
        {
            RetriedStatusCodes.Add(httpStatusCode);
            base.TraceHttpRequestRetrying(activity, request, attempt, backoffDuration, httpStatusCode, webExceptionStatus, socketErrorCode, winHttpErrorCode, curlErrorCode, afdRefInfo);
        }
    }

    private sealed class HeadersReceivedHandler : DelegatingHandler
    {
        private readonly TaskCompletionSource<bool> _headersReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public HeadersReceivedHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        public Task HeadersReceived => _headersReceived.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _headersReceived.TrySetResult(true);
            return response;
        }
    }

    private sealed class ResponseServer : IAsyncDisposable
    {
        public static readonly TimeSpan DripInterval = TimeSpan.FromMilliseconds(100);

        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _responseStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _chunksSent;

        public ResponseServer(byte[] response = null, int dripChunks = 0, bool finishBody = false, byte[] delayedBody = null)
        {
            _listener.Start();
            Address = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
            ConnectionClosed = ServeAsync(response, dripChunks, finishBody, delayedBody);
        }

        public Uri Address { get; }
        public string RequestHeaders { get; private set; }
        public Task ResponseStarted => _responseStarted.Task;
        public TaskCompletionSource<bool> ReleaseBody { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task ConnectionClosed { get; }
        public int ChunksSent => Volatile.Read(ref _chunksSent);

        private async Task ServeAsync(byte[] response, int dripChunks, bool finishBody, byte[] delayedBody)
        {
            using TcpClient connection = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false);
            using NetworkStream stream = connection.GetStream();
            var buffer = new byte[1];
            RequestHeaders = await ReadRequestAsync(stream, _shutdown.Token).ConfigureAwait(false);

            response ??= CreateResponse("Content-Length: 2\r\n", RequestHeaders.StartsWith("HEAD ", StringComparison.Ordinal) ? "" : "{");
            await stream.WriteAsync(response, _shutdown.Token).ConfigureAwait(false);
            _responseStarted.TrySetResult(true);

            if (delayedBody != null)
            {
                await ReleaseBody.Task.WaitAsync(_shutdown.Token).ConfigureAwait(false);
                await stream.WriteAsync(delayedBody, _shutdown.Token).ConfigureAwait(false);
                return;
            }

            Task<int> disconnected = stream.ReadAsync(buffer, _shutdown.Token).AsTask();
            if (dripChunks > 0)
            {
                byte[] oneByteChunk = Encoding.ASCII.GetBytes("1\r\n \r\n");
                while (ChunksSent < dripChunks && !disconnected.IsCompleted)
                {
                    await Task.WhenAny(disconnected, Task.Delay(DripInterval, _shutdown.Token)).ConfigureAwait(false);
                    _shutdown.Token.ThrowIfCancellationRequested();
                    if (disconnected.IsCompleted || !await WriteUnlessPeerClosedAsync(stream, oneByteChunk).ConfigureAwait(false))
                    {
                        break;
                    }

                    Interlocked.Increment(ref _chunksSent);
                }

                if (finishBody && !disconnected.IsCompleted)
                {
                    byte[] endOfChunkedBody = Encoding.ASCII.GetBytes("0\r\n\r\n");
                    await WriteUnlessPeerClosedAsync(stream, endOfChunkedBody).ConfigureAwait(false);
                }
            }

            await AssertClosedByPeerAsync(disconnected).ConfigureAwait(false);
        }

        private async Task<bool> WriteUnlessPeerClosedAsync(NetworkStream stream, byte[] data)
        {
            try
            {
                await stream.WriteAsync(data, _shutdown.Token).ConfigureAwait(false);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();
            try
            {
                await ConnectionClosed.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
            }
            finally
            {
                _shutdown.Dispose();
            }
        }
    }

    private sealed class Reply
    {
        public Reply(byte[] response, bool stallBody = false, Task resetAfter = null)
        {
            Response = response;
            StallBody = stallBody;
            ResetAfter = resetAfter;
        }

        public byte[] Response { get; }
        public bool StallBody { get; }
        public Task ResetAfter { get; }
        public TaskCompletionSource<bool> Closed { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class SequenceServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly Task _completed;

        private int _requestCount;

        public SequenceServer(params Reply[] repliesInConnectionOrder)
        {
            _listener.Start();
            Address = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
            _completed = ServeAsync(repliesInConnectionOrder);
        }

        public Uri Address { get; }
        public int RequestCount => Volatile.Read(ref _requestCount);

        private async Task ServeAsync(Reply[] repliesInConnectionOrder)
        {
            var connectionsServedConcurrently = new List<Task>();
            try
            {
                foreach (Reply reply in repliesInConnectionOrder)
                {
                    TcpClient connection = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false);
                    connectionsServedConcurrently.Add(ServeConnectionAsync(connection, reply));
                }
            }
            finally
            {
                await Task.WhenAll(connectionsServedConcurrently).ConfigureAwait(false);
            }
        }

        private async Task ServeConnectionAsync(TcpClient connection, Reply reply)
        {
            using (connection)
            {
                await ServeReplyAsync(connection, reply).ConfigureAwait(false);
            }

            reply.Closed.TrySetResult(true);
        }

        private async Task ServeReplyAsync(TcpClient connection, Reply reply)
        {
            using NetworkStream stream = connection.GetStream();
            await ReadRequestAsync(stream, _shutdown.Token).ConfigureAwait(false);
            Interlocked.Increment(ref _requestCount);
            await stream.WriteAsync(reply.Response, _shutdown.Token).ConfigureAwait(false);

            if (reply.ResetAfter != null)
            {
                await reply.ResetAfter.WaitAsync(_shutdown.Token).ConfigureAwait(false);
                ResetConnection(connection);
            }
            else if (reply.StallBody)
            {
                await AssertClosedByPeerAsync(stream.ReadAsync(new byte[1], _shutdown.Token).AsTask()).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();
            try
            {
                await _completed.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
            }
            finally
            {
                _shutdown.Dispose();
            }
        }
    }
}
