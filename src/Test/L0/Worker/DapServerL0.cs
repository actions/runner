using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Worker.Dap;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapServerL0
    {
        private DapServer _server;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _server = new DapServer();
            _server.Initialize(hc);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeSucceeds()
        {
            using (CreateTestContext())
            {
                Assert.NotNull(_server);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetSessionAcceptsMock()
        {
            using (CreateTestContext())
            {
                var mockSession = new Mock<IDapDebugSession>();
                _server.SetSession(mockSession.Object);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SendEventNoClientDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var evt = new Event
                {
                    EventType = "stopped",
                    Body = new StoppedEventBody
                    {
                        Reason = "entry",
                        ThreadId = 1,
                        AllThreadsStopped = true
                    }
                };

                _server.SendEvent(evt);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SendResponseNoClientDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var response = new Response
                {
                    Type = "response",
                    RequestSeq = 1,
                    Command = "initialize",
                    Success = true
                };

                _server.SendResponse(response);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SendMessageNoClientDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var msg = new ProtocolMessage
                {
                    Type = "response",
                    Seq = 1
                };

                _server.SendMessage(msg);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StopWithoutStartDoesNotThrow()
        {
            using (CreateTestContext())
            {
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopOnAvailablePort()
        {
            using (CreateTestContext())
            {
                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitForConnectionCancelledByCancellationToken()
        {
            using (CreateTestContext())
            {
                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);

                var waitTask = _server.WaitForConnectionAsync(cts.Token);

                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await waitTask;
                });

                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StartAndStopMultipleTimesDoesNotThrow()
        {
            using (CreateTestContext())
            {
                var cts1 = new CancellationTokenSource();
                await _server.StartAsync(0, cts1.Token);
                await _server.StopAsync();

                _server = new DapServer();
                _server.Initialize(CreateTestContext());
                var cts2 = new CancellationTokenSource();
                await _server.StartAsync(0, cts2.Token);
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task MessageFraming_ValidMessage_ProcessedSuccessfully()
        {
            using (var hc = CreateTestContext())
            {
                var receivedMessages = new List<string>();
                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, CancellationToken>((json, ct) => receivedMessages.Add(json))
                    .Returns(Task.CompletedTask);
                _server.SetSession(mockSession.Object);

                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);

                var listenerField = typeof(DapServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
                var listener = (TcpListener)listenerField.GetValue(_server);
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                var stream = client.GetStream();

                // Wait for server to accept connection
                await Task.Delay(100);

                // Send a valid DAP request with Content-Length framing
                var requestJson = "{\"seq\":1,\"type\":\"request\",\"command\":\"initialize\"}";
                var body = Encoding.UTF8.GetBytes(requestJson);
                var header = $"Content-Length: {body.Length}\r\n\r\n";
                var headerBytes = Encoding.ASCII.GetBytes(header);

                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                await stream.WriteAsync(body, 0, body.Length);
                await stream.FlushAsync();

                // Wait for processing
                await Task.Delay(500);

                Assert.Single(receivedMessages);
                Assert.Contains("initialize", receivedMessages[0]);

                cts.Cancel();
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CentralizedMasking_SecretsInResponseAreMasked()
        {
            using (var hc = CreateTestContext())
            {
                // Register a secret
                hc.SecretMasker.AddValue("super-secret-token");

                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);

                var listenerField = typeof(DapServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
                var listener = (TcpListener)listenerField.GetValue(_server);
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                var stream = client.GetStream();

                await Task.Delay(100);

                // Send a response that contains the secret (through the server API)
                var response = new Response
                {
                    Type = "response",
                    RequestSeq = 1,
                    Command = "evaluate",
                    Success = true,
                    Body = new EvaluateResponseBody
                    {
                        Result = "The value is super-secret-token here",
                        Type = "string",
                        VariablesReference = 0
                    }
                };

                _server.SendResponse(response);

                // Read what the client received
                await Task.Delay(200);
                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // The response should NOT contain the raw secret
                Assert.DoesNotContain("super-secret-token", received);
                // It should contain the masked version
                Assert.Contains("***", received);

                cts.Cancel();
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CentralizedMasking_SecretsInEventsAreMasked()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue("event-secret-value");

                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);

                var listenerField = typeof(DapServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
                var listener = (TcpListener)listenerField.GetValue(_server);
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                var stream = client.GetStream();

                await Task.Delay(100);

                _server.SendEvent(new Event
                {
                    EventType = "output",
                    Body = new OutputEventBody
                    {
                        Category = "stdout",
                        Output = "Output contains event-secret-value here"
                    }
                });

                await Task.Delay(200);
                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.DoesNotContain("event-secret-value", received);
                Assert.Contains("***", received);

                cts.Cancel();
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StopAsync_AwaitsConnectionLoopShutdown()
        {
            using (CreateTestContext())
            {
                var cts = new CancellationTokenSource();
                await _server.StartAsync(0, cts.Token);

                // Stop should complete within a reasonable time
                var stopTask = _server.StopAsync();
                var completed = await Task.WhenAny(stopTask, Task.Delay(10000));
                Assert.Equal(stopTask, completed);
            }
        }
    }
}
