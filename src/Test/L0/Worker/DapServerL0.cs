using System;
using System.Collections.Generic;
using System.IO;
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
            }

            using (CreateTestContext($"{nameof(StartAndStopMultipleTimesDoesNotThrow)}_SecondStart"))
            {
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
                var messageReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var mockSession = new Mock<IDapDebugSession>();
                mockSession.Setup(x => x.HandleMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, CancellationToken>((json, ct) => messageReceived.TrySetResult(json))
                    .Returns(Task.CompletedTask);
                _server.SetSession(mockSession.Object);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _server.StartAsync(0, cts.Token);

                var listenerField = typeof(DapServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
                var listener = (TcpListener)listenerField.GetValue(_server);
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                var connectionTask = _server.WaitForConnectionAsync(cts.Token);
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                await connectionTask;
                var stream = client.GetStream();

                // Send a valid DAP request with Content-Length framing
                var requestJson = "{\"seq\":1,\"type\":\"request\",\"command\":\"initialize\"}";
                var body = Encoding.UTF8.GetBytes(requestJson);
                var header = $"Content-Length: {body.Length}\r\n\r\n";
                var headerBytes = Encoding.ASCII.GetBytes(header);

                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                await stream.WriteAsync(body, 0, body.Length);
                await stream.FlushAsync();

                // Wait for session to receive the message (deterministic, bounded)
                var completed = await Task.WhenAny(messageReceived.Task, Task.Delay(5000));
                Assert.Equal(messageReceived.Task, completed);
                Assert.Contains("initialize", await messageReceived.Task);

                cts.Cancel();
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ProtocolMetadata_PreservedWhenSecretCollidesWithKeywords()
        {
            using (var hc = CreateTestContext())
            {
                // Register secrets that match DAP protocol keywords
                hc.SecretMasker.AddValue("response");
                hc.SecretMasker.AddValue("output");
                hc.SecretMasker.AddValue("evaluate");

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _server.StartAsync(0, cts.Token);

                var listenerField = typeof(DapServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
                var listener = (TcpListener)listenerField.GetValue(_server);
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                var connectionTask = _server.WaitForConnectionAsync(cts.Token);
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                await connectionTask;
                var stream = client.GetStream();

                // Send a response whose protocol fields collide with secrets
                var response = new Response
                {
                    Type = "response",
                    RequestSeq = 1,
                    Command = "evaluate",
                    Success = true,
                    Body = new EvaluateResponseBody
                    {
                        Result = "some result",
                        Type = "string",
                        VariablesReference = 0
                    }
                };

                _server.SendResponse(response);

                // Read a full framed DAP message with timeout
                var received = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));

                // Protocol metadata MUST be preserved even when secrets collide
                Assert.Contains("\"type\":\"response\"", received);
                Assert.Contains("\"command\":\"evaluate\"", received);
                Assert.Contains("\"success\":true", received);

                cts.Cancel();
                await _server.StopAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ProtocolMetadata_EventFieldsPreservedWhenSecretCollidesWithKeywords()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue("output");
                hc.SecretMasker.AddValue("stdout");

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _server.StartAsync(0, cts.Token);

                var listenerField = typeof(DapServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
                var listener = (TcpListener)listenerField.GetValue(_server);
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                var connectionTask = _server.WaitForConnectionAsync(cts.Token);
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                await connectionTask;
                var stream = client.GetStream();

                _server.SendEvent(new Event
                {
                    EventType = "output",
                    Body = new OutputEventBody
                    {
                        Category = "stdout",
                        Output = "hello world"
                    }
                });

                // Read a full framed DAP message with timeout
                var received = await ReadDapMessageAsync(stream, TimeSpan.FromSeconds(5));

                // Protocol fields MUST be preserved
                Assert.Contains("\"type\":\"event\"", received);
                Assert.Contains("\"event\":\"output\"", received);
                Assert.Contains("\"category\":\"stdout\"", received);

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

        /// <summary>
        /// Reads a single DAP-framed message from a stream with a timeout.
        /// Parses the Content-Length header, reads exactly that many bytes,
        /// and returns the JSON body. Fails with a clear error on timeout.
        /// </summary>
        private static async Task<string> ReadDapMessageAsync(NetworkStream stream, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            // Read headers byte-by-byte until we see \r\n\r\n
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
                if (headers.EndsWith("\r\n\r\n"))
                {
                    // Parse Content-Length
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

            // Read exactly contentLength bytes
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
    }
}
