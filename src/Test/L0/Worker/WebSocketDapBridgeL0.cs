using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Worker.Dap;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class WebSocketDapBridgeL0
    {
        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            return new TestHostContext(this, testName);
        }

        private static async Task<byte[]> ReadWebSocketMessageAsync(ClientWebSocket client, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            using var buffer = new MemoryStream();
            var receiveBuffer = new byte[1024];

            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new EndOfStreamException("WebSocket closed unexpectedly.");
                }

                if (result.Count > 0)
                {
                    buffer.Write(receiveBuffer, 0, result.Count);
                }

                if (result.EndOfMessage)
                {
                    return buffer.ToArray();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BridgeForwardsWebSocketFramesToTcpAndBack()
        {
            using var hc = CreateTestContext();
            using var targetListener = new TcpListener(IPAddress.Loopback, 0);
            targetListener.Start();

            var targetPort = ((IPEndPoint)targetListener.LocalEndpoint).Port;

            var bridge = new WebSocketDapBridge();
            bridge.Initialize(hc);
            bridge.Start(0, targetPort);
            var bridgePort = bridge.ListenPort;

            try
            {
            var echoTask = Task.Run(async () =>
            {
                using var targetClient = await targetListener.AcceptTcpClientAsync();
                using var stream = targetClient.GetStream();
                
                var headerBuilder = new StringBuilder();
                var buffer = new byte[1];
                var contentLength = -1;

                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, 1);
                    if (bytesRead == 0) break;

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

                var body = new byte[contentLength];
                var totalRead = 0;
                while (totalRead < contentLength)
                {
                    var bytesRead = await stream.ReadAsync(body, totalRead, contentLength - totalRead);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                var header = $"Content-Length: {body.Length}\r\n\r\n";
                var headerBytes = Encoding.ASCII.GetBytes(header);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                await stream.WriteAsync(body, 0, body.Length);
                await stream.FlushAsync();
            });

            using var client = new ClientWebSocket();
            client.Options.Proxy = null;
            await client.ConnectAsync(new Uri($"ws://127.0.0.1:{bridgePort}/"), CancellationToken.None);

            var dapMessage = "{\"type\":\"request\",\"seq\":1,\"command\":\"initialize\"}";
            var payload = Encoding.UTF8.GetBytes(dapMessage);
            await client.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

            var echoed = await ReadWebSocketMessageAsync(client, TimeSpan.FromSeconds(5));
            Assert.Equal(payload, echoed);

            await echoTask;
            }
            finally
            {
                await bridge.ShutdownAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BridgeRejectsNonWebSocketRequests()
        {
            using var hc = CreateTestContext();

            var bridge = new WebSocketDapBridge();
            bridge.Initialize(hc);
            bridge.Start(0, 0);
            var bridgePort = bridge.ListenPort;

            try
            {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, bridgePort);
            using var stream = client.GetStream();

            var request = Encoding.ASCII.GetBytes(
                "GET / HTTP/1.1\r\n" +
                "Host: localhost\r\n" +
                "\r\n");
            await stream.WriteAsync(request, 0, request.Length);
            await stream.FlushAsync();

            // Read until the server closes the connection (Connection: close).
            // A single ReadAsync may return a partial response on some platforms.
            using var ms = new MemoryStream();
            var responseBuffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length)) > 0)
            {
                ms.Write(responseBuffer, 0, bytesRead);
            }

            var response = Encoding.ASCII.GetString(ms.ToArray());

            Assert.Contains("400 BadRequest", response);
            Assert.Contains("Expected a websocket upgrade request.", response);
            }
            finally
            {
                await bridge.ShutdownAsync();
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ' }, 1)]
        [InlineData(new byte[] { 0x81, 0x85, 0x00, 0x00 }, 2)]
        [InlineData(new byte[] { 0xC1, 0x85, 0x00, 0x00 }, 3)]
        [InlineData(new byte[] { (byte)'P', (byte)'R', (byte)'I', (byte)' ' }, 4)]
        [InlineData(new byte[] { 0x16, 0x03, 0x03, 0x01 }, 5)]
        [InlineData(new byte[] { (byte)'B', (byte)'A', (byte)'D', (byte)'!' }, 0)]
        public void ClassifyIncomingStreamPrefixDetectsExpectedProtocols(byte[] initialBytes, int expectedKind)
        {
            var actualKind = WebSocketDapBridge.ClassifyIncomingStreamPrefix(initialBytes);
            Assert.Equal((WebSocketDapBridge.IncomingStreamPrefixKind)expectedKind, actualKind);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BridgeRejectsOversizedWebSocketMessage()
        {
            using var hc = CreateTestContext();
            using var targetListener = new TcpListener(IPAddress.Loopback, 0);
            targetListener.Start();

            var targetPort = ((IPEndPoint)targetListener.LocalEndpoint).Port;

            var bridge = new WebSocketDapBridge();
            bridge.Initialize(hc);
            bridge.MaxInboundMessageSize = 64; // artificially small limit for testing
            bridge.Start(0, targetPort);
            var bridgePort = bridge.ListenPort;

            try
            {
            using var client = new ClientWebSocket();
            client.Options.Proxy = null;
            await client.ConnectAsync(new Uri($"ws://127.0.0.1:{bridgePort}/"), CancellationToken.None);

            // Send a message that exceeds the 64-byte limit
            var oversizedPayload = new byte[128];
            Array.Fill(oversizedPayload, (byte)'X');
            await client.SendAsync(
                new ArraySegment<byte>(oversizedPayload),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);

            // The bridge should close the connection with MessageTooBig
            var receiveBuffer = new byte[256];
            using var receiveCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await client.ReceiveAsync(
                new ArraySegment<byte>(receiveBuffer),
                receiveCts.Token);

            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.MessageTooBig, client.CloseStatus);
            }
            finally
            {
                await bridge.ShutdownAsync();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BridgeShutdownCompletesWhenPeerDoesNotCloseGracefully()
        {
            using var hc = CreateTestContext();
            using var targetListener = new TcpListener(IPAddress.Loopback, 0);
            targetListener.Start();

            var targetPort = ((IPEndPoint)targetListener.LocalEndpoint).Port;

            var bridge = new WebSocketDapBridge();
            bridge.Initialize(hc);
            bridge.Start(0, targetPort);
            var bridgePort = bridge.ListenPort;

            // Connect a raw TCP client but never perform WebSocket close handshake
            using var rawClient = new TcpClient();
            await rawClient.ConnectAsync(IPAddress.Loopback, bridgePort);

            // Shutdown should complete within a bounded time, not hang
            var shutdownTask = bridge.ShutdownAsync();
            var completed = await Task.WhenAny(shutdownTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.True(completed == shutdownTask, "Bridge shutdown should complete within the timeout, not hang on a non-cooperative peer");
        }
    }
}
