using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapDebuggerL0
    {
        private const string TimeoutEnvironmentVariable = "ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT";
        private const string TunnelConnectTimeoutVariable = "ACTIONS_RUNNER_DAP_TUNNEL_CONNECT_TIMEOUT_SECONDS";
        private DapDebugger _debugger;
        private TestWebSocketDapBridge _testWebSocketBridge;

        private sealed class TestWebSocketDapBridge : RunnerService, IWebSocketDapBridge
        {
            private readonly WebSocketDapBridge _inner = new WebSocketDapBridge();

            public int ListenPort => _inner.ListenPort;

            public override void Initialize(IHostContext hostContext)
            {
                base.Initialize(hostContext);
                _inner.Initialize(hostContext);
            }

            public void Start(int listenPort, int targetPort)
            {
                _inner.Start(0, targetPort);
            }

            public Task ShutdownAsync()
            {
                return _inner.ShutdownAsync();
            }
        }

        private TestHostContext CreateTestContext(bool enableWebSocketBridge = false, [CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _debugger = new DapDebugger();
            _testWebSocketBridge = null;
            _debugger.Initialize(hc);
            _debugger.SkipTunnelRelay = true;
            _debugger.SkipWebSocketBridge = !enableWebSocketBridge;
            if (enableWebSocketBridge)
            {
                _testWebSocketBridge = new TestWebSocketDapBridge();
                hc.EnqueueInstance<IWebSocketDapBridge>(_testWebSocketBridge);
            }

            return hc;
        }

        private static async Task WithEnvironmentVariableAsync(string name, string value, Func<Task> action)
        {
            var originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            try
            {
                await action();
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, originalValue);
            }
        }

        private static void WithEnvironmentVariable(string name, string value, Action action)
        {
            var originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            try
            {
                action();
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, originalValue);
            }
        }

        private static ushort GetFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        }

        private static async Task<TcpClient> ConnectClientAsync(int port)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            return client;
        }

        private static async Task<ClientWebSocket> ConnectWebSocketClientAsync(int port)
        {
            var client = new ClientWebSocket();
            client.Options.Proxy = null;
            await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
            return client;
        }

        private static async Task SendRequestAsync(NetworkStream stream, Request request)
        {
            var json = JsonConvert.SerializeObject(request);
            var body = Encoding.UTF8.GetBytes(json);
            var header = $"Content-Length: {body.Length}\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);

            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }

        private static async Task SendRequestAsync(WebSocket client, Request request)
        {
            var json = JsonConvert.SerializeObject(request);
            var body = Encoding.UTF8.GetBytes(json);

            await client.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }

        /// <summary>
        /// Reads a single DAP-framed message from a stream with a timeout.
        /// Parses the Content-Length header, reads exactly that many bytes,
        /// and returns the JSON body. Fails with a clear error on timeout.
        /// </summary>
        private static async Task<string> ReadDapMessageAsync(NetworkStream stream, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            var headerBuilder = new StringBuilder();
            var buffer = new byte[1];
            var contentLength = -1;

            while (true)
            {
                var readTask = stream.ReadAsync(buffer, 0, 1, token);
                var bytesRead = await readTask;
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Connection closed while reading DAP headers");
                }

                headerBuilder.Append((char)buffer[0]);
                var headers = headerBuilder.ToString();
                if (headers.EndsWith("\r\n\r\n", StringComparison.Ordinal))
                {
                    foreach (var line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.StartsWith("Content-Length: ", StringComparison.OrdinalIgnoreCase))
                        {
                            contentLength = int.Parse(line.Substring("Content-Length: ".Length).Trim());
                        }
                    }
                    break;
                }
            }

            if (contentLength < 0)
            {
                throw new InvalidOperationException("No Content-Length header found in DAP message");
            }

            var body = new byte[contentLength];
            var totalRead = 0;
            while (totalRead < contentLength)
            {
                var bytesRead = await stream.ReadAsync(body, totalRead, contentLength - totalRead, token);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Connection closed while reading DAP body");
                }
                totalRead += bytesRead;
            }

            return Encoding.UTF8.GetString(body);
        }

        private static async Task<string> ReadWebSocketDataUntilAsync(WebSocket client, TimeSpan timeout, params string[] expectedFragments)
        {
            using var cts = new CancellationTokenSource(timeout);
            var buffer = new byte[4096];
            var allMessages = new StringBuilder();

            while (true)
            {
                using var messageStream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new EndOfStreamException("WebSocket closed before expected DAP messages were received.");
                    }

                    if (result.Count > 0)
                    {
                        messageStream.Write(buffer, 0, result.Count);
                    }
                }
                while (!result.EndOfMessage);

                var messageText = Encoding.UTF8.GetString(messageStream.ToArray());
                allMessages.Append(messageText);

                var text = allMessages.ToString();
                var containsAllFragments = true;
                foreach (var fragment in expectedFragments)
                {
                    if (!text.Contains(fragment, StringComparison.Ordinal))
                    {
                        containsAllFragments = false;
                        break;
                    }
                }

                if (containsAllFragments)
                {
                    return text;
                }
            }
        }

        private static Mock<IExecutionContext> CreateJobContextWithTunnel(CancellationToken cancellationToken, ushort port, string jobName = null, bool overrideWelcomeMessage = false, string welcomeMessage = null)
        {
            var tunnel = new GitHub.DistributedTask.Pipelines.DebuggerTunnelInfo
            {
                TunnelId = "test-tunnel",
                ClusterId = "test-cluster",
                HostToken = "test-token",
                Port = port
            };
            var debuggerConfig = new DebuggerConfig(true, tunnel, overrideWelcomeMessage, welcomeMessage);
            var jobContext = new Mock<IExecutionContext>();
            jobContext.Setup(x => x.CancellationToken).Returns(cancellationToken);
            jobContext.Setup(x => x.Global).Returns(new GlobalContext { Debugger = debuggerConfig });
            jobContext
                .Setup(x => x.GetGitHubContext(It.IsAny<string>()))
                .Returns((string contextName) => string.Equals(contextName, "job", StringComparison.Ordinal) ? jobName : null);
            return jobContext;
        }

        private static Mock<IStep> CreateStep(string displayName, ActionRunStage? stage = null)
        {
            var step = new Mock<IStep>();
            step.Setup(s => s.DisplayName).Returns(displayName);
            if (stage.HasValue)
            {
                var executionContext = new Mock<IExecutionContext>();
                executionContext.Setup(x => x.Stage).Returns(stage.Value);
                step.Setup(s => s.ExecutionContext).Returns(executionContext.Object);
            }
            else
            {
                step.Setup(s => s.ExecutionContext).Returns((IExecutionContext)null);
            }

            return step;
        }

        private static Mock<IActionRunner> CreateActionRunner(string displayName, ActionRunStage stage, Pipelines.ActionStep action)
        {
            var executionContext = new Mock<IExecutionContext>();
            executionContext.Setup(x => x.Stage).Returns(stage);

            var runner = new Mock<IActionRunner>();
            runner.Setup(s => s.DisplayName).Returns(displayName);
            runner.Setup(s => s.ExecutionContext).Returns(executionContext.Object);
            runner.Setup(s => s.Stage).Returns(stage);
            runner.Setup(s => s.Action).Returns(action);
            return runner;
        }

        private static Pipelines.ActionStep CreateRepositoryActionStep(string name)
        {
            return new Pipelines.ActionStep
            {
                Id = Guid.NewGuid(),
                Name = name,
                Reference = new Pipelines.RepositoryPathReference
                {
                    Name = name,
                    Ref = "v1",
                    RepositoryType = Pipelines.RepositoryTypes.GitHub
                }
            };
        }

        private static Definition CreateActionDefinitionWithPost()
        {
            return new Definition
            {
                Data = new ActionDefinitionData
                {
                    Execution = new NodeJSActionExecutionData
                    {
                        Script = "main.js",
                        Post = "post.js"
                    }
                }
            };
        }

        private static Request MakeRequest(string command, object arguments)
        {
            return new Request
            {
                Seq = 1,
                Type = "request",
                Command = command,
                Arguments = JObject.FromObject(arguments)
            };
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeSucceeds()
        {
            using (CreateTestContext())
            {
                Assert.NotNull(_debugger);
                Assert.False(_debugger.IsActive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAsyncFailsWithoutValidTunnelConfig()
        {
            using (CreateTestContext())
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = new Mock<IExecutionContext>();
                jobContext.Setup(x => x.CancellationToken).Returns(cts.Token);
                jobContext.Setup(x => x.Global).Returns(new GlobalContext
                {
                    Debugger = new DebuggerConfig(true, null)
                });

                await Assert.ThrowsAsync<ArgumentException>(() => _debugger.StartAsync(jobContext.Object));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAsyncUsesPortFromTunnelConfig()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);
                using var client = await ConnectClientAsync(port);
                Assert.True(client.Connected);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAsyncWithWebSocketBridgeAcceptsInitializeOverWebSocket()
        {
            using (CreateTestContext(enableWebSocketBridge: true))
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, GetFreePort());
                await _debugger.StartAsync(jobContext.Object);

                var bridgePort = _testWebSocketBridge.ListenPort;
                Assert.NotEqual(0, _debugger.InternalDapPort);
                Assert.NotEqual(0, bridgePort);
                Assert.NotEqual(bridgePort, _debugger.InternalDapPort);

                using var client = await ConnectWebSocketClientAsync(bridgePort);
                await SendRequestAsync(client, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "initialize"
                });

                var response = await ReadWebSocketDataUntilAsync(
                    client,
                    TimeSpan.FromSeconds(5),
                    "\"type\":\"response\"",
                    "\"command\":\"initialize\"",
                    "\"event\":\"initialized\"");

                Assert.Contains("\"success\":true", response);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAsyncWithWebSocketBridgeAcceptsPreUpgradedWebSocketStream()
        {
            using (CreateTestContext(enableWebSocketBridge: true))
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, GetFreePort());
                await _debugger.StartAsync(jobContext.Object);

                var bridgePort = _testWebSocketBridge.ListenPort;
                Assert.NotEqual(0, _debugger.InternalDapPort);
                Assert.NotEqual(0, bridgePort);
                Assert.NotEqual(bridgePort, _debugger.InternalDapPort);

                using var tcpClient = await ConnectClientAsync(bridgePort);
                using var webSocket = WebSocket.CreateFromStream(
                    tcpClient.GetStream(),
                    isServer: false,
                    subProtocol: null,
                    keepAliveInterval: TimeSpan.FromSeconds(30));

                await SendRequestAsync(webSocket, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "initialize"
                });

                var response = await ReadWebSocketDataUntilAsync(
                    webSocket,
                    TimeSpan.FromSeconds(5),
                    "\"type\":\"response\"",
                    "\"command\":\"initialize\"",
                    "\"event\":\"initialized\"");

                Assert.Contains("\"success\":true", response);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTimeoutUsesCustomTimeoutFromEnvironment()
        {
            using (CreateTestContext())
            {
                WithEnvironmentVariable(TimeoutEnvironmentVariable, "30", () =>
                {
                    Assert.Equal(30, _debugger.ResolveTimeout());
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTimeoutIgnoresInvalidTimeoutFromEnvironment()
        {
            using (CreateTestContext())
            {
                WithEnvironmentVariable(TimeoutEnvironmentVariable, "not-a-number", () =>
                {
                    Assert.Equal(15, _debugger.ResolveTimeout());
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTimeoutIgnoresZeroTimeoutFromEnvironment()
        {
            using (CreateTestContext())
            {
                WithEnvironmentVariable(TimeoutEnvironmentVariable, "0", () =>
                {
                    Assert.Equal(15, _debugger.ResolveTimeout());
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopLifecycle()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);
                using var client = await ConnectClientAsync(port);
                Assert.True(client.Connected);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopMultipleTimesDoesNotThrow()
        {
            using (CreateTestContext())
            {
                foreach (var port in new[] { GetFreePort(), GetFreePort() })
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                    await _debugger.StartAsync(jobContext.Object);
                    await _debugger.StopAsync();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyCompletesAfterClientConnectionAndConfigurationDone()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                await SendRequestAsync(client.GetStream(), new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                await waitTask;
                Assert.Equal(DapSessionState.Ready, _debugger.State);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartStoresJobContextForThreadsRequest()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port, "ci-job");
                await _debugger.StartAsync(jobContext.Object);
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(client.GetStream(), new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "threads"
                });

                var response = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"threads\"", response);
                Assert.Contains("\"name\":\"Job: ci-job\"", response);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancellationUnblocksAndOnJobCompletedTerminates()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                await SendRequestAsync(client.GetStream(), new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                await waitTask;
                cts.Cancel();

                // In the real runner, JobRunner always calls OnJobCompletedAsync
                // from a finally block. The cancellation callback only unblocks
                // pending waits; OnJobCompletedAsync handles state + cleanup.
                await _debugger.OnJobCompletedAsync();
                Assert.Equal(DapSessionState.Terminated, _debugger.State);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StopWithoutStartDoesNotThrow()
        {
            using (CreateTestContext())
            {
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnJobCompletedTerminatesSession()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                await SendRequestAsync(client.GetStream(), new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                await waitTask;
                await _debugger.OnJobCompletedAsync();
                Assert.Equal(DapSessionState.Terminated, _debugger.State);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyBeforeStartIsNoOp()
        {
            using (CreateTestContext())
            {
                await _debugger.WaitUntilReadyAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitUntilReadyJobCancellationPropagatesAsOperationCancelledException()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                await Task.Delay(50);
                cts.Cancel();

                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
                Assert.IsNotType<TimeoutException>(ex);
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task InitializeRequestOverSocketPreservesProtocolMetadataWhenSecretsCollide()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue("response");
                hc.SecretMasker.AddValue("initialize");
                hc.SecretMasker.AddValue("event");
                hc.SecretMasker.AddValue("initialized");

                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();

                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "initialize"
                });

                var response = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"type\":\"response\"", response);
                Assert.Contains("\"command\":\"initialize\"", response);
                Assert.Contains("\"success\":true", response);

                var initializedEvent = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"type\":\"event\"", initializedEvent);
                Assert.Contains("\"event\":\"initialized\"", initializedEvent);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancellationDuringStepPauseReleasesWait()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                // Complete handshake so session is ready
                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });
                await waitTask;

                // Simulate a step starting (which pauses)
                var step = new Mock<IStep>();
                step.Setup(s => s.DisplayName).Returns("Test Step");
                step.Setup(s => s.ExecutionContext).Returns((IExecutionContext)null);
                var stepTask = _debugger.OnStepStartingAsync(step.Object);

                // Give the step time to pause
                await Task.Delay(50);

                // Cancel the job — should release the step pause
                cts.Cancel();
                await stepTask;

                // In the real runner, OnJobCompletedAsync always follows.
                await _debugger.OnJobCompletedAsync();
                Assert.Equal(DapSessionState.Terminated, _debugger.State);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StopAsyncSafeAtAnyLifecyclePoint()
        {
            using (CreateTestContext())
            {
                // StopAsync before start
                await _debugger.StopAsync();

                // Start then immediate stop (no connection, no WaitUntilReady)
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);
                await _debugger.StopAsync();

                // StopAsync after already stopped
                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task HandleSourceReturnsJobStepsSource()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue("secret-step");
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var pre = CreateStep("Pre cache", ActionRunStage.Pre);
                var checkout = CreateStep("Checkout");
                var secret = CreateStep("secret-step");
                var post = CreateStep("Post cache", ActionRunStage.Post);
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { pre.Object, checkout.Object, secret.Object },
                    new[] { post.Object });

                var response = _debugger.HandleSource(MakeRequest(
                    "source",
                    new SourceArguments { SourceReference = 1 }));

                Assert.True(response.Success);
                var body = Assert.IsType<SourceResponseBody>(response.Body);
                Assert.Equal(
                    "pre:\n  - step: \"Set up job\"\n  - step: \"Pre cache\"\n\nmain:\n  - step: \"Checkout\"\n  - step: \"***\"\n\npost:\n  - step: \"Post cache\"\n  - step: \"Complete job\"\n",
                    body.Content);
                Assert.Null(body.MimeType);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StackTraceUsesJobStepsSourceLine()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var checkout = CreateStep("Checkout");
                var build = CreateStep("Build");
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { checkout.Object, build.Object },
                    Array.Empty<IStep>());

                var stepTask = _debugger.OnStepStartingAsync(build.Object);
                var stoppedEvent = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"stopped\"", stoppedEvent);

                var bannerEvent = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"output\"", bannerEvent);

                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "stackTrace"
                });

                var stackTraceJson = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var stackTrace = JObject.Parse(stackTraceJson);
                var frame = stackTrace["body"]?["stackFrames"]?[0];

                Assert.NotNull(frame);
                Assert.Equal(6, frame["line"].Value<int>());
                Assert.Equal(1, frame["source"]["sourceReference"].Value<int>());
                Assert.Equal("execution.yml", frame["source"]["name"].Value<string>());

                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "continue"
                });
                await stepTask;

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StackTraceOmitsSourceForUnmappedCurrentStep()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var checkout = CreateStep("Checkout");
                var build = CreateStep("Build");
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { checkout.Object },
                    Array.Empty<IStep>());

                var stepTask = _debugger.OnStepStartingAsync(build.Object);
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));

                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "stackTrace"
                });

                var stackTraceJson = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var stackTrace = JObject.Parse(stackTraceJson);
                var frame = stackTrace["body"]?["stackFrames"]?[0];

                Assert.NotNull(frame);
                Assert.Equal(0, frame["line"].Value<int>());
                Assert.Null(frame["source"]);

                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "continue"
                });
                await stepTask;

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task PredictedPostStepIsServedAtInitializationAndClaimedAtRegistration()
        {
            using (var hc = CreateTestContext())
            {
                var action = CreateRepositoryActionStep("actions/cache");
                var actionManager = new Mock<IActionManager>();
                actionManager
                    .Setup(x => x.LoadAction(It.IsAny<IExecutionContext>(), action))
                    .Returns(CreateActionDefinitionWithPost());
                hc.SetSingleton<IActionManager>(actionManager.Object);

                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var checkout = CreateActionRunner("Checkout", ActionRunStage.Main, action);
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { checkout.Object },
                    Array.Empty<IStep>());

                var sourceResponse = _debugger.HandleSource(MakeRequest(
                    "source",
                    new SourceArguments { SourceReference = 1 }));
                var sourceBody = Assert.IsType<SourceResponseBody>(sourceResponse.Body);
                Assert.Equal(
                    "pre:\n  - step: \"Set up job\"\n\nmain:\n  - step: \"Checkout\"\n\npost:\n  - step: \"Post Checkout\"\n  - step: \"Complete job\"\n",
                    sourceBody.Content);

                var post = CreateActionRunner("Post Checkout", ActionRunStage.Post, action);
                _debugger.OnPostStepRegistered(post.Object);

                var stepTask = _debugger.OnStepStartingAsync(post.Object);
                var stoppedEvent = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"stopped\"", stoppedEvent);

                var bannerEvent = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"output\"", bannerEvent);

                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "stackTrace"
                });

                var stackTraceJson = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var stackTrace = JObject.Parse(stackTraceJson);
                var frame = stackTrace["body"]?["stackFrames"]?[0];

                Assert.NotNull(frame);
                Assert.Equal(8, frame["line"].Value<int>());
                Assert.Equal(1, frame["source"]["sourceReference"].Value<int>());

                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "continue"
                });
                await stepTask;

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StackTraceSanitizesSyntheticSourcePath()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port, jobName: "my/job\\name");
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var checkout = CreateStep("Checkout");
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { checkout.Object },
                    Array.Empty<IStep>());

                var stepTask = _debugger.OnStepStartingAsync(checkout.Object);
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));

                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "stackTrace"
                });

                var stackTraceJson = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var stackTrace = JObject.Parse(stackTraceJson);
                var frame = stackTrace["body"]?["stackFrames"]?[0];

                Assert.NotNull(frame);
                Assert.Equal("my_job_name/execution.yml", frame["source"]["path"].Value<string>());

                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "continue"
                });
                await stepTask;

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnJobCompletedSendsTerminatedAndExitedEvents()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                // Read the configurationDone response
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                // Read the welcome message output event
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var checkout = CreateStep("Checkout");
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { checkout.Object },
                    Array.Empty<IStep>());

                // Complete the job — OnJobCompletedAsync pauses when stepping,
                // so run it in the background and send continue to unblock.
                var completedTask = _debugger.OnJobCompletedAsync();

                // Read the stopped event from the pause
                var stoppedMsg = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"stopped\"", stoppedMsg);

                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "stackTrace"
                });

                var stackTraceJson = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var stackTrace = JObject.Parse(stackTraceJson);
                var frame = stackTrace["body"]?["stackFrames"]?[0];

                Assert.NotNull(frame);
                Assert.Equal("Complete job [completed]", frame["name"].Value<string>());
                Assert.Equal(8, frame["line"].Value<int>());
                Assert.Equal(1, frame["source"]["sourceReference"].Value<int>());

                // Send continue to unblock the pause
                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "continue"
                });

                await completedTask;

                // Read remaining messages — continue response + continued event + terminated + exited
                var allMessages = new System.Text.StringBuilder();
                for (int i = 0; i < 4; i++)
                {
                    allMessages.Append(await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5)));
                }

                var combined = allMessages.ToString();
                Assert.Contains("\"event\":\"terminated\"", combined);
                Assert.Contains("\"event\":\"exited\"", combined);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnJobCompletedUsesSyntheticCompleteJobLineWhenPostStepSharesName()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                var checkout = CreateStep("Checkout");
                var realPost = CreateStep("Complete job", ActionRunStage.Post);
                await _debugger.OnJobStepsInitializedAsync(
                    new[] { checkout.Object },
                    new[] { realPost.Object });

                var completedTask = _debugger.OnJobCompletedAsync();

                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));

                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "stackTrace"
                });

                var stackTraceJson = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var stackTrace = JObject.Parse(stackTraceJson);
                var frame = stackTrace["body"]?["stackFrames"]?[0];

                Assert.NotNull(frame);
                Assert.Equal("Complete job [completed]", frame["name"].Value<string>());
                Assert.Equal(9, frame["line"].Value<int>());

                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "continue"
                });

                await completedTask;
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTunnelConnectTimeoutReturnsDefaultWhenNoVariable()
        {
            using (CreateTestContext())
            {
                Assert.Equal(30, _debugger.ResolveTunnelConnectTimeout());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTunnelConnectTimeoutUsesCustomValue()
        {
            using (CreateTestContext())
            {
                WithEnvironmentVariable(TunnelConnectTimeoutVariable, "60", () =>
                {
                    Assert.Equal(60, _debugger.ResolveTunnelConnectTimeout());
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTunnelConnectTimeoutIgnoresInvalidValue()
        {
            using (CreateTestContext())
            {
                WithEnvironmentVariable(TunnelConnectTimeoutVariable, "not-a-number", () =>
                {
                    Assert.Equal(30, _debugger.ResolveTunnelConnectTimeout());
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveTunnelConnectTimeoutIgnoresZeroValue()
        {
            using (CreateTestContext())
            {
                WithEnvironmentVariable(TunnelConnectTimeoutVariable, "0", () =>
                {
                    Assert.Equal(30, _debugger.ResolveTunnelConnectTimeout());
                });
            }
        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitForCommandAsyncUnblocksOnCancellationDuringWait()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                var waitTask = _debugger.WaitUntilReadyAsync();
                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                // Read the welcome message output event
                await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                await waitTask;

                // Start OnJobCompletedAsync — it will pause because _pauseOnNextStep is true
                var completedTask = _debugger.OnJobCompletedAsync();

                // Read the stopped event
                var stoppedMsg = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"stopped\"", stoppedMsg);

                // Cancel the job while waiting — should unblock the pause
                cts.Cancel();

                // OnJobCompletedAsync should complete without hanging
                var finished = await Task.WhenAny(completedTask, Task.Delay(TimeSpan.FromSeconds(5)));
                Assert.Equal(completedTask, finished);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WelcomeMessageSendsDefaultHelpWhenOverrideDisabled()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();

                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                // First message: configurationDone response
                var configDoneResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"configurationDone\"", configDoneResponse);

                // Second message: welcome output event with default help text
                var welcomeMsg = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"output\"", welcomeMsg);
                Assert.Contains("\"category\":\"console\"", welcomeMsg);
                Assert.Contains("Actions Debug Console", welcomeMsg);
                Assert.Contains("help", welcomeMsg);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WelcomeMessageShowsCustomMessageWhenOverrideEnabled()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port,
                    overrideWelcomeMessage: true,
                    welcomeMessage: "Welcome to debugging!");
                await _debugger.StartAsync(jobContext.Object);

                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();

                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                // First: configurationDone response
                var configDoneResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"configurationDone\"", configDoneResponse);

                // Second: custom welcome message
                var welcomeMsg = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"output\"", welcomeMsg);
                Assert.Contains("Welcome to debugging!", welcomeMsg);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WelcomeMessageSuppressedWhenOverrideEnabledWithEmptyMessage()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port,
                    overrideWelcomeMessage: true,
                    welcomeMessage: "");
                await _debugger.StartAsync(jobContext.Object);

                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();

                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                // Read configurationDone response
                var configDoneResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"configurationDone\"", configDoneResponse);

                // Send threads request — if welcome message was suppressed, this
                // should be the next response (no output event in between)
                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "threads"
                });

                var threadsResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"threads\"", threadsResponse);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WelcomeMessageSuppressedWhenOverrideEnabledWithNullMessage()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port,
                    overrideWelcomeMessage: true,
                    welcomeMessage: null);
                await _debugger.StartAsync(jobContext.Object);

                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();

                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                // Read configurationDone response
                var configDoneResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"configurationDone\"", configDoneResponse);

                // Send threads request — if welcome message was suppressed, this
                // should be the next response (no output event in between)
                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "threads"
                });

                var threadsResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"threads\"", threadsResponse);

                await _debugger.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WelcomeMessageSentOnlyOnce()
        {
            using (CreateTestContext())
            {
                var port = GetFreePort();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var jobContext = CreateJobContextWithTunnel(cts.Token, port);
                await _debugger.StartAsync(jobContext.Object);

                using var client = await ConnectClientAsync(port);
                var stream = client.GetStream();

                // First configurationDone
                await SendRequestAsync(stream, new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "configurationDone"
                });

                var configDoneResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"configurationDone\"", configDoneResponse);

                // Welcome message should appear
                var welcomeMsg = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"event\":\"output\"", welcomeMsg);
                Assert.Contains("Actions Debug Console", welcomeMsg);

                // Second configurationDone — should NOT produce another welcome message
                await SendRequestAsync(stream, new Request
                {
                    Seq = 2,
                    Type = "request",
                    Command = "configurationDone"
                });

                var secondResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"configurationDone\"", secondResponse);

                // Next message should be threads response, not another welcome output
                await SendRequestAsync(stream, new Request
                {
                    Seq = 3,
                    Type = "request",
                    Command = "threads"
                });

                var threadsResponse = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                Assert.Contains("\"command\":\"threads\"", threadsResponse);

                await _debugger.StopAsync();
            }
        }
    }
}
