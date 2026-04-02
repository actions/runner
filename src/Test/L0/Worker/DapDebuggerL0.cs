using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Newtonsoft.Json;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapDebuggerL0
    {
        private const string TimeoutEnvironmentVariable = "ACTIONS_RUNNER_DAP_CONNECTION_TIMEOUT";
        private const string TunnelConnectTimeoutVariable = "ACTIONS_RUNNER_DAP_TUNNEL_CONNECT_TIMEOUT_SECONDS";
        private DapDebugger _debugger;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _debugger = new DapDebugger();
            _debugger.Initialize(hc);
            _debugger.SkipTunnelRelay = true;
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

        private static Mock<IExecutionContext> CreateJobContextWithTunnel(CancellationToken cancellationToken, ushort port, string jobName = null)
        {
            var tunnel = new GitHub.DistributedTask.Pipelines.DebuggerTunnelInfo
            {
                TunnelId = "test-tunnel",
                ClusterId = "test-cluster",
                HostToken = "test-token",
                Port = port
            };
            var debuggerConfig = new DebuggerConfig(true, tunnel);
            var jobContext = new Mock<IExecutionContext>();
            jobContext.Setup(x => x.CancellationToken).Returns(cancellationToken);
            jobContext.Setup(x => x.Global).Returns(new GlobalContext { Debugger = debuggerConfig });
            jobContext
                .Setup(x => x.GetGitHubContext(It.IsAny<string>()))
                .Returns((string contextName) => string.Equals(contextName, "job", StringComparison.Ordinal) ? jobName : null);
            return jobContext;
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
                await waitTask;

                // Complete the job — events are sent via OnJobCompletedAsync
                await _debugger.OnJobCompletedAsync();

                var msg1 = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));
                var msg2 = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));

                // Both events should arrive (order may vary)
                var combined = msg1 + msg2;
                Assert.Contains("\"event\":\"terminated\"", combined);
                Assert.Contains("\"event\":\"exited\"", combined);
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
    }
}
