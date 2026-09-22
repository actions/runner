using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using Xunit;

namespace GitHub.Actions.RunService.WebApi.Tests;

// Verifies that ClientWebSocket.ConnectAsync(Uri, HttpMessageInvoker, ...), reusing BrokerHttpClient's
// authenticated pipeline (RawHttpMessageHandler), actually applies the OAuth Authorization header to the
// websocket handshake request -- the same mechanism CheckBrokerLongPollWebSocketAsync in Runner.Worker
// relies on to authenticate the runner long-poll websocket probe against broker-listener.
public sealed class BrokerHttpClientWebSocketL0
{
    private const string TestAccessToken = "test-broker-websocket-probe-token";

    [Fact]
    public async Task ConnectRunnerLongPollWebSocketAsyncAppliesAuthorizationHeader()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var acceptTask = AcceptWebSocketHandshakeAsync(listener);

        var credential = new VssOAuthAccessTokenCredential(TestAccessToken);
        var handler = new RawHttpMessageHandler(credential, new RawClientHttpRequestSettings());

        using var client = new BrokerHttpClient(
            new Uri($"ws://127.0.0.1:{port}/_ws/ping.sock"),
            handler,
            disposeHandler: true);

        using var socket = await client.ConnectRunnerLongPollWebSocketAsync(CancellationToken.None);

        var requestHeaders = await acceptTask;
        listener.Stop();

        Assert.True(
            requestHeaders.TryGetValue("Authorization", out var authorizationHeader),
            $"Handshake request did not include an Authorization header. Headers seen: {string.Join(", ", requestHeaders.Keys)}");
        Assert.Contains(TestAccessToken, authorizationHeader);
    }

    // Accepts a single TCP connection, reads the raw HTTP handshake request headers,
    // completes the websocket upgrade (so ConnectAsync succeeds), and returns the headers seen.
    private static async Task<Dictionary<string, string>> AcceptWebSocketHandshakeAsync(TcpListener listener)
    {
        using var tcpClient = await listener.AcceptTcpClientAsync();
        using var stream = tcpClient.GetStream();

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = new System.IO.StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true))
        {
            // Skip the request line (e.g. "GET /_ws/ping.sock HTTP/1.1").
            await reader.ReadLineAsync();

            string line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var name = line.Substring(0, separatorIndex).Trim();
                    var value = line.Substring(separatorIndex + 1).Trim();
                    headers[name] = value;
                }
            }
        }

        headers.TryGetValue("Sec-WebSocket-Key", out var webSocketKey);
        var accept = Convert.ToBase64String(
            SHA1.HashData(Encoding.ASCII.GetBytes(webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

        var response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            $"Sec-WebSocket-Accept: {accept}\r\n\r\n");
        await stream.WriteAsync(response);

        return headers;
    }
}
