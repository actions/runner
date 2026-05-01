using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    internal sealed class WebSocketDapBridge : RunnerService, IWebSocketDapBridge
    {
        internal enum IncomingStreamPrefixKind
        {
            Unknown,
            HttpWebSocketUpgrade,
            PreUpgradedWebSocket,
            WebSocketReservedBits,
            Http2Preface,
            TlsClientHello,
        }

        private const int _bufferSize = 32 * 1024;
        private const int _maxHeaderLineLength = 8 * 1024;
        private const int _defaultMaxInboundMessageSize = 10 * 1024 * 1024; // 10 MB
        private static readonly TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan _handshakeTimeout = TimeSpan.FromSeconds(10);
        private const string _webSocketAcceptMagic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private const int _maxHeaderCount = 64;
        private static readonly byte[] _headerEndMarker = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        private int _listenPort;
        private int _targetPort;

        private TcpListener _listener;
        private CancellationTokenSource _loopCts;
        private Task _acceptLoopTask;

        public int MaxInboundMessageSize { get; set; } = _defaultMaxInboundMessageSize;

        internal int ListenPort => (_listener?.LocalEndpoint as IPEndPoint)?.Port ?? 0;

        public void Start(int listenPort, int targetPort)
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("WebSocket DAP bridge already started.");
            }

            _listenPort = listenPort;
            _targetPort = targetPort;

            _listener = new TcpListener(IPAddress.Loopback, _listenPort);
            _listener.Start();
            _loopCts = new CancellationTokenSource();
            _acceptLoopTask = AcceptLoopAsync(_loopCts.Token);

            Trace.Info($"WebSocket DAP bridge listening on {_listener.LocalEndpoint} -> 127.0.0.1:{_targetPort}");
        }

        public async Task ShutdownAsync()
        {
            _loopCts?.Cancel();

            try
            {
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                Trace.Warning($"Error stopping listener during shutdown ({ex.GetType().Name})");
            }

            if (_acceptLoopTask != null)
            {
                try
                {
                    await _acceptLoopTask;
                }
                catch (OperationCanceledException)
                {
                    // expected on shutdown
                }
            }

            _loopCts?.Dispose();
            _loopCts = null;
            _listener = null;
            _acceptLoopTask = null;
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    client.NoDelay = true;
                    await HandleClientAsync(client, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    client?.Dispose();
                    Trace.Error($"WebSocket DAP bridge connection error");
                    Trace.Error(ex);
                }
                finally
                {
                    client?.Dispose();
                }
            }

            Trace.Info("WebSocket DAP bridge accept loop ended");
        }

        private async Task HandleClientAsync(TcpClient incomingClient, CancellationToken cancellationToken)
        {
            using (var incomingStream = incomingClient.GetStream())
            {
                Trace.Info($"WebSocket DAP bridge accepted client {incomingClient.Client.RemoteEndPoint}");

                WebSocket webSocket;
                using (var handshakeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    handshakeCts.CancelAfter(_handshakeTimeout);
                    try
                    {
                        webSocket = await AcceptWebSocketAsync(incomingStream, handshakeCts.Token);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        Trace.Warning("WebSocket handshake timed out");
                        return;
                    }
                }
                if (webSocket == null)
                {
                    return;
                }

                using (webSocket)
                using (var dapClient = new TcpClient())
                {
                    dapClient.NoDelay = true;
                    await dapClient.ConnectAsync(IPAddress.Loopback, _targetPort, cancellationToken);

                    using (var dapStream = dapClient.GetStream())
                    using (var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        var proxyToken = sessionCts.Token;
                        var wsToTcpTask = PumpWebSocketToTcpAsync(webSocket, dapStream, proxyToken);
                        var tcpToWsTask = PumpTcpToWebSocketAsync(dapStream, webSocket, proxyToken);

                        await Task.WhenAny(wsToTcpTask, tcpToWsTask);
                        sessionCts.Cancel();

                        await CloseWebSocketAsync(webSocket);

                        try
                        {
                            await Task.WhenAll(wsToTcpTask, tcpToWsTask);
                        }
                        catch (OperationCanceledException) when (proxyToken.IsCancellationRequested)
                        {
                            // expected during shutdown
                        }
                        catch (Exception ex)
                        {
                            Trace.Warning($"DAP protocol error: {ex}");
                        }
                    }
                }
            }
        }

        private async Task<WebSocket> AcceptWebSocketAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var initialBytes = await ReadInitialBytesAsync(stream, cancellationToken);
            if (initialBytes == null || initialBytes.Length == 0)
            {
                return null;
            }

            var prefixKind = ClassifyIncomingStreamPrefix(initialBytes);
            if (prefixKind == IncomingStreamPrefixKind.PreUpgradedWebSocket)
            {
                Trace.Info($"Treating incoming tunnel stream as an already-upgraded websocket connection ({DescribeInitialBytes(initialBytes)})");
                return WebSocket.CreateFromStream(
                    new ReplayableStream(stream, initialBytes),
                    isServer: true,
                    subProtocol: null,
                    keepAliveInterval: _keepAliveInterval);
            }

            if (prefixKind != IncomingStreamPrefixKind.HttpWebSocketUpgrade)
            {
                Trace.Warning($"Unsupported debugger tunnel stream prefix ({prefixKind}): {DescribeInitialBytes(initialBytes)}");
                return null;
            }

            var handshakeStream = new ReplayableStream(stream, initialBytes);
            var requestLine = await ReadLineAsync(handshakeStream, cancellationToken);
            if (string.IsNullOrEmpty(requestLine))
            {
                return null;
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (headers.Count >= _maxHeaderCount)
                {
                    Trace.Warning($"Rejected WebSocket request with too many headers (>{_maxHeaderCount})");
                    await WriteHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Too many headers.", cancellationToken);
                    return null;
                }

                var line = await ReadLineAsync(handshakeStream, cancellationToken);
                if (line == null)
                {
                    return null;
                }

                if (line.Length == 0)
                {
                    break;
                }

                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    await WriteHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Invalid HTTP header.", cancellationToken);
                    return null;
                }

                var headerName = line.Substring(0, separatorIndex).Trim();
                var headerValue = line.Substring(separatorIndex + 1).Trim();

                if (headers.TryGetValue(headerName, out var existingValue))
                {
                    headers[headerName] = $"{existingValue}, {headerValue}";
                }
                else
                {
                    headers[headerName] = headerValue;
                }
            }

            if (!IsValidWebSocketRequest(requestLine, headers))
            {
                var method = requestLine.Split(' ')[0];
                Trace.Info($"Rejected non-websocket request (method={method})");
                await WriteHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Expected a websocket upgrade request.", cancellationToken);
                return null;
            }

            if (!headers.TryGetValue("Sec-WebSocket-Version", out var webSocketVersion) ||
                !string.Equals(webSocketVersion.Trim(), "13", StringComparison.Ordinal))
            {
                Trace.Warning("Rejected WebSocket request with unsupported version");
                await WriteHttpErrorAsync(stream, (HttpStatusCode)426, "Unsupported WebSocket version. Expected: 13.", cancellationToken);
                return null;
            }

            var webSocketKey = headers["Sec-WebSocket-Key"];
            if (!IsValidWebSocketKey(webSocketKey))
            {
                Trace.Warning("Rejected WebSocket request with invalid Sec-WebSocket-Key");
                await WriteHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Invalid Sec-WebSocket-Key.", cancellationToken);
                return null;
            }

            var acceptValue = ComputeAcceptValue(webSocketKey);
            var responseBytes = Encoding.ASCII.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                $"Sec-WebSocket-Accept: {acceptValue}\r\n" +
                "\r\n");

            await handshakeStream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
            await handshakeStream.FlushAsync(cancellationToken);

            Trace.Info("WebSocket DAP bridge completed websocket handshake");
            return WebSocket.CreateFromStream(handshakeStream, isServer: true, subProtocol: null, keepAliveInterval: _keepAliveInterval);
        }

        private async Task PumpWebSocketToTcpAsync(WebSocket source, NetworkStream destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[_bufferSize];

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var messageStream = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        if (result.MessageType != WebSocketMessageType.Binary &&
                            result.MessageType != WebSocketMessageType.Text)
                        {
                            break;
                        }

                        if (result.Count > 0)
                        {
                            if (messageStream.Length + result.Count > MaxInboundMessageSize)
                            {
                                Trace.Warning($"WebSocket message exceeds maximum allowed size of {MaxInboundMessageSize} bytes, closing connection");
                                await source.CloseAsync(
                                    WebSocketCloseStatus.MessageTooBig,
                                    $"Message exceeds {MaxInboundMessageSize} byte limit",
                                    CancellationToken.None);
                                return;
                            }

                            messageStream.Write(buffer, 0, result.Count);
                        }
                    }
                    while (!result.EndOfMessage && !cancellationToken.IsCancellationRequested);

                    if (result.MessageType != WebSocketMessageType.Binary &&
                        result.MessageType != WebSocketMessageType.Text)
                    {
                        continue;
                    }

                    var messageBytes = messageStream.ToArray();
                    if (messageBytes.Length == 0)
                    {
                        continue;
                    }

                    var contentLengthHeader = Encoding.ASCII.GetBytes($"Content-Length: {messageBytes.Length}\r\n\r\n");
                    await destination.WriteAsync(contentLengthHeader, 0, contentLengthHeader.Length, cancellationToken);
                    await destination.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                    await destination.FlushAsync(cancellationToken);
                }
            }
        }

        private static async Task PumpTcpToWebSocketAsync(NetworkStream source, WebSocket destination, CancellationToken cancellationToken)
        {
            var readBuffer = new byte[_bufferSize];
            var dapBuffer = new List<byte>();

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await source.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                dapBuffer.AddRange(new ArraySegment<byte>(readBuffer, 0, bytesRead));

                while (TryParseDapMessage(dapBuffer, out var messageBody))
                {
                    await destination.SendAsync(
                        new ArraySegment<byte>(messageBody),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken);
                }
            }
        }

        private static bool TryParseDapMessage(List<byte> buffer, out byte[] messageBody)
        {
            messageBody = null;

            var headerEndIndex = FindSequence(buffer, _headerEndMarker);
            if (headerEndIndex == -1)
            {
                return false;
            }

            var headerBytes = buffer.GetRange(0, headerEndIndex).ToArray();
            var headerText = Encoding.ASCII.GetString(headerBytes);

            var contentLength = -1;
            foreach (var line in headerText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    var valueStart = line.IndexOf(':') + 1;
                    if (int.TryParse(line.Substring(valueStart).Trim(), out var parsedLength))
                    {
                        contentLength = parsedLength;
                        break;
                    }
                }
            }

            if (contentLength < 0)
            {
                throw new InvalidOperationException("DAP message missing or unparseable Content-Length header; tearing down session.");
            }

            var messageStart = headerEndIndex + 4;
            var messageEnd = messageStart + contentLength;

            if (buffer.Count < messageEnd)
            {
                return false;
            }

            messageBody = buffer.GetRange(messageStart, contentLength).ToArray();
            buffer.RemoveRange(0, messageEnd);
            return true;
        }

        private static int FindSequence(List<byte> buffer, byte[] sequence)
        {
            if (buffer.Count < sequence.Length)
            {
                return -1;
            }

            for (int i = 0; i <= buffer.Count - sequence.Length; i++)
            {
                var match = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (buffer[i + j] != sequence[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsValidWebSocketRequest(string requestLine, IDictionary<string, string> headers)
        {
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                return false;
            }

            var requestLineParts = requestLine.Split(' ');
            if (requestLineParts.Length < 3 || !string.Equals(requestLineParts[0], "GET", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return HeaderContainsToken(headers, "Connection", "Upgrade") &&
                HeaderContainsToken(headers, "Upgrade", "websocket") &&
                headers.ContainsKey("Sec-WebSocket-Key");
        }

        private static bool HeaderContainsToken(IDictionary<string, string> headers, string headerName, string expectedToken)
        {
            if (!headers.TryGetValue(headerName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
            {
                return false;
            }

            return headerValue
                .Split(',')
                .Select(token => token.Trim())
                .Any(token => string.Equals(token, expectedToken, StringComparison.OrdinalIgnoreCase));
        }

        private static string ComputeAcceptValue(string webSocketKey)
        {
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes($"{webSocketKey}{_webSocketAcceptMagic}");
                var hashBytes = sha1.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static bool IsValidWebSocketKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            {
                return false;
            }

            try
            {
                var decoded = Convert.FromBase64String(key);
                return decoded.Length == 16;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static async Task<string> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
        {
            var lineBuilder = new StringBuilder();
            var buffer = new byte[1];
            var previousWasCarriageReturn = false;

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, 1, cancellationToken);
                if (bytesRead == 0)
                {
                    return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
                }

                var currentChar = (char)buffer[0];
                if (currentChar == '\n' && previousWasCarriageReturn)
                {
                    if (lineBuilder.Length > 0 && lineBuilder[lineBuilder.Length - 1] == '\r')
                    {
                        lineBuilder.Length--;
                    }

                    return lineBuilder.ToString();
                }

                previousWasCarriageReturn = currentChar == '\r';
                lineBuilder.Append(currentChar);

                if (lineBuilder.Length > _maxHeaderLineLength)
                {
                    throw new InvalidDataException($"HTTP header line exceeds maximum length of {_maxHeaderLineLength}");
                }
            }
        }

        private static async Task<byte[]> ReadInitialBytesAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[4];
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var bytesRead = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
            }

            if (totalRead == 0)
            {
                return Array.Empty<byte>();
            }

            if (totalRead == buffer.Length)
            {
                return buffer;
            }

            var initialBytes = new byte[totalRead];
            Array.Copy(buffer, initialBytes, totalRead);
            return initialBytes;
        }

        internal static IncomingStreamPrefixKind ClassifyIncomingStreamPrefix(byte[] initialBytes)
        {
            if (LooksLikeHttpUpgrade(initialBytes))
            {
                return IncomingStreamPrefixKind.HttpWebSocketUpgrade;
            }

            if (LooksLikeHttp2Preface(initialBytes))
            {
                return IncomingStreamPrefixKind.Http2Preface;
            }

            if (LooksLikeTlsClientHello(initialBytes))
            {
                return IncomingStreamPrefixKind.TlsClientHello;
            }

            if (LooksLikeWebSocketFramePrefix(initialBytes, requireReservedBitsClear: false))
            {
                return HasReservedBitsSet(initialBytes[0])
                    ? IncomingStreamPrefixKind.WebSocketReservedBits
                    : IncomingStreamPrefixKind.PreUpgradedWebSocket;
            }

            return IncomingStreamPrefixKind.Unknown;
        }

        internal static string DescribeInitialBytes(byte[] initialBytes)
        {
            if (initialBytes == null || initialBytes.Length == 0)
            {
                return "no bytes read";
            }

            var hex = BitConverter.ToString(initialBytes);
            var ascii = new string(initialBytes.Select(value => value >= 32 && value <= 126 ? (char)value : '.').ToArray());
            return $"hex={hex}, ascii=\"{ascii}\"";
        }

        private static bool LooksLikeHttpUpgrade(byte[] initialBytes)
        {
            if (initialBytes == null || initialBytes.Length < 4)
            {
                return false;
            }

            return initialBytes[0] == (byte)'G' &&
                initialBytes[1] == (byte)'E' &&
                initialBytes[2] == (byte)'T' &&
                initialBytes[3] == (byte)' ';
        }

        private static bool LooksLikeHttp2Preface(byte[] initialBytes)
        {
            if (initialBytes == null || initialBytes.Length < 4)
            {
                return false;
            }

            return initialBytes[0] == (byte)'P' &&
                initialBytes[1] == (byte)'R' &&
                initialBytes[2] == (byte)'I' &&
                initialBytes[3] == (byte)' ';
        }

        private static bool LooksLikeTlsClientHello(byte[] initialBytes)
        {
            if (initialBytes == null || initialBytes.Length < 3)
            {
                return false;
            }

            return initialBytes[0] == 0x16 &&
                initialBytes[1] == 0x03 &&
                initialBytes[2] >= 0x00 &&
                initialBytes[2] <= 0x04;
        }

        private static bool LooksLikeWebSocketFramePrefix(byte[] initialBytes, bool requireReservedBitsClear)
        {
            if (initialBytes == null || initialBytes.Length < 2)
            {
                return false;
            }

            var firstByte = initialBytes[0];
            var secondByte = initialBytes[1];
            var opcode = firstByte & 0x0F;
            var isMasked = (secondByte & 0x80) != 0;

            if (!isMasked || !IsSupportedWebSocketOpcode(opcode))
            {
                return false;
            }

            return !requireReservedBitsClear || !HasReservedBitsSet(firstByte);
        }

        private static bool HasReservedBitsSet(byte firstByte)
        {
            return (firstByte & 0x70) != 0;
        }

        private static bool IsSupportedWebSocketOpcode(int opcode)
        {
            switch (opcode)
            {
                case 0x0:
                case 0x1:
                case 0x2:
                case 0x8:
                case 0x9:
                case 0xA:
                    return true;
                default:
                    return false;
            }
        }

        private static async Task WriteHttpErrorAsync(
            NetworkStream stream,
            HttpStatusCode statusCode,
            string message,
            CancellationToken cancellationToken)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(message);
            var responseBytes = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n" +
                "Connection: close\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "\r\n");

            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private static async Task CloseWebSocketAsync(WebSocket webSocket)
        {
            if (webSocket == null)
            {
                return;
            }

            if (webSocket.State != WebSocketState.Open &&
                webSocket.State != WebSocketState.CloseReceived)
            {
                return;
            }

            try
            {
                using var cts = new CancellationTokenSource(_closeTimeout);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Graceful close timed out, abort the connection.
                webSocket.Abort();
            }
            catch (WebSocketException)
            {
                // Peer already disconnected.
            }
        }

        private sealed class ReplayableStream : Stream
        {
            private readonly Stream _innerStream;
            private readonly byte[] _prefixBytes;
            private int _prefixOffset;

            public ReplayableStream(Stream innerStream, byte[] prefixBytes)
            {
                _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
                _prefixBytes = prefixBytes ?? Array.Empty<byte>();
            }

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => _innerStream.CanWrite;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => _innerStream.Flush();

            public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (TryReadPrefix(buffer, offset, count, out var bytesRead))
                {
                    return bytesRead;
                }

                return _innerStream.Read(buffer, offset, count);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (TryReadPrefix(buffer, offset, count, out var bytesRead))
                {
                    return bytesRead;
                }

                return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_prefixOffset < _prefixBytes.Length)
                {
                    var bytesToCopy = Math.Min(buffer.Length, _prefixBytes.Length - _prefixOffset);
                    new ReadOnlySpan<byte>(_prefixBytes, _prefixOffset, bytesToCopy).CopyTo(buffer.Span);
                    _prefixOffset += bytesToCopy;
                    return bytesToCopy;
                }

                return await _innerStream.ReadAsync(buffer, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
                _innerStream.WriteAsync(buffer, cancellationToken);

            private bool TryReadPrefix(byte[] buffer, int offset, int count, out int bytesRead)
            {
                if (_prefixOffset >= _prefixBytes.Length)
                {
                    bytesRead = 0;
                    return false;
                }

                bytesRead = Math.Min(count, _prefixBytes.Length - _prefixOffset);
                Array.Copy(_prefixBytes, _prefixOffset, buffer, offset, bytesRead);
                _prefixOffset += bytesRead;
                return true;
            }
        }
    }
}
