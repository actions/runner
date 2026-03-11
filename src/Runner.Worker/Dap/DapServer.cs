using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using Newtonsoft.Json;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Production TCP server for the Debug Adapter Protocol.
    /// Handles Content-Length message framing, JSON serialization,
    /// client reconnection, and graceful shutdown.
    /// </summary>
    public sealed class DapServer : RunnerService, IDapServer
    {
        private const string ContentLengthHeader = "Content-Length: ";

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private IDapDebugSession _session;
        private CancellationTokenSource _cts;
        private TaskCompletionSource<bool> _connectionTcs;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private int _nextSeq = 1;
        private volatile bool _acceptConnections = true;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            Trace.Info("DapServer initialized");
        }

        public void SetSession(IDapDebugSession session)
        {
            _session = session;
            Trace.Info("Debug session set");
        }

        public async Task StartAsync(int port, CancellationToken cancellationToken)
        {
            Trace.Info($"Starting DAP server on port {port}");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _connectionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Trace.Info($"DAP server listening on 127.0.0.1:{port}");

            // Start the connection loop in the background
            _ = ConnectionLoopAsync(_cts.Token);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Accepts client connections in a loop, supporting reconnection.
        /// When a client disconnects, the server waits for a new connection
        /// without blocking step execution.
        /// </summary>
        private async Task ConnectionLoopAsync(CancellationToken cancellationToken)
        {
            while (_acceptConnections && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Trace.Info("Waiting for debug client connection...");

                    using (cancellationToken.Register(() =>
                    {
                        try { _listener?.Stop(); }
                        catch { /* listener already stopped */ }
                    }))
                    {
                        _client = await _listener.AcceptTcpClientAsync();
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _stream = _client.GetStream();
                    var remoteEndPoint = _client.Client.RemoteEndPoint;
                    Trace.Info($"Debug client connected from {remoteEndPoint}");

                    // Signal first connection (no-op on subsequent connections)
                    _connectionTcs.TrySetResult(true);

                    // Notify session of new client
                    _session?.HandleClientConnected();

                    // Process messages until client disconnects
                    await ProcessMessagesAsync(cancellationToken);

                    // Client disconnected — notify session and clean up
                    Trace.Info("Client disconnected, waiting for reconnection...");
                    _session?.HandleClientDisconnected();
                    CleanupConnection();
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.Warning($"Connection error: {ex.Message}");
                    CleanupConnection();

                    if (!_acceptConnections || cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Brief delay before accepting next connection
                    try
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _connectionTcs.TrySetCanceled();
            Trace.Info("Connection loop ended");
        }

        /// <summary>
        /// Cleans up the current client connection without stopping the listener.
        /// </summary>
        private void CleanupConnection()
        {
            try { _stream?.Close(); } catch { /* best effort */ }
            try { _client?.Close(); } catch { /* best effort */ }
            _stream = null;
            _client = null;
        }

        public async Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            Trace.Info("Waiting for debug client to connect...");

            using (cancellationToken.Register(() => _connectionTcs.TrySetCanceled()))
            {
                await _connectionTcs.Task;
            }

            Trace.Info("Debug client connected");
        }

        public async Task StopAsync()
        {
            Trace.Info("Stopping DAP server");

            _acceptConnections = false;
            _cts?.Cancel();

            CleanupConnection();

            try { _listener?.Stop(); }
            catch { /* best effort */ }

            await Task.CompletedTask;

            Trace.Info("DAP server stopped");
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            Trace.Info("Starting DAP message processing loop");

            try
            {
                while (!cancellationToken.IsCancellationRequested && _client?.Connected == true)
                {
                    var json = await ReadMessageAsync(cancellationToken);
                    if (json == null)
                    {
                        Trace.Info("Client disconnected (end of stream)");
                        break;
                    }

                    await ProcessSingleMessageAsync(json, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Trace.Info("Message processing cancelled");
            }
            catch (IOException ex)
            {
                Trace.Info($"Connection closed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.Error($"Error in message loop: {ex}");
            }

            Trace.Info("DAP message processing loop ended");
        }

        private async Task ProcessSingleMessageAsync(string json, CancellationToken cancellationToken)
        {
            Request request = null;
            try
            {
                request = JsonConvert.DeserializeObject<Request>(json);
                if (request == null || request.Type != "request")
                {
                    Trace.Warning($"Received non-request message: {json}");
                    return;
                }

                Trace.Info($"Received request: seq={request.Seq}, command={request.Command}");

                if (_session == null)
                {
                    Trace.Error("No debug session configured");
                    SendErrorResponse(request, "No debug session configured");
                    return;
                }

                // Pass raw JSON to session — session handles deserialization, dispatch,
                // and calls back to SendResponse when done.
                await _session.HandleMessageAsync(json, cancellationToken);
            }
            catch (JsonException ex)
            {
                Trace.Error($"Failed to parse request: {ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.Error($"Error processing request: {ex}");
                if (request != null)
                {
                    SendErrorResponse(request, ex.Message);
                }
            }
        }

        private void SendErrorResponse(Request request, string message)
        {
            var response = new Response
            {
                Type = "response",
                RequestSeq = request.Seq,
                Command = request.Command,
                Success = false,
                Message = message,
                Body = new ErrorResponseBody
                {
                    Error = new Message
                    {
                        Id = 1,
                        Format = message,
                        ShowUser = true
                    }
                }
            };

            SendResponse(response);
        }

        /// <summary>
        /// Reads a DAP message using Content-Length framing.
        /// Format: Content-Length: N\r\n\r\n{json}
        /// </summary>
        private async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            int contentLength = -1;

            while (true)
            {
                var line = await ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    return null;
                }

                if (line.Length == 0)
                {
                    break;
                }

                if (line.StartsWith(ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
                {
                    var lengthStr = line.Substring(ContentLengthHeader.Length).Trim();
                    if (!int.TryParse(lengthStr, out contentLength))
                    {
                        throw new InvalidDataException($"Invalid Content-Length: {lengthStr}");
                    }
                }
            }

            if (contentLength < 0)
            {
                throw new InvalidDataException("Missing Content-Length header");
            }

            var buffer = new byte[contentLength];
            var totalRead = 0;
            while (totalRead < contentLength)
            {
                var bytesRead = await _stream.ReadAsync(buffer, totalRead, contentLength - totalRead, cancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Connection closed while reading message body");
                }
                totalRead += bytesRead;
            }

            var json = Encoding.UTF8.GetString(buffer);
            Trace.Verbose($"Received: {json}");
            return json;
        }

        /// <summary>
        /// Reads a line terminated by \r\n from the network stream.
        /// </summary>
        private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            var lineBuilder = new StringBuilder();
            var buffer = new byte[1];
            var previousWasCr = false;

            while (true)
            {
                var bytesRead = await _stream.ReadAsync(buffer, 0, 1, cancellationToken);
                if (bytesRead == 0)
                {
                    return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
                }

                var c = (char)buffer[0];

                if (c == '\n' && previousWasCr)
                {
                    if (lineBuilder.Length > 0 && lineBuilder[lineBuilder.Length - 1] == '\r')
                    {
                        lineBuilder.Length--;
                    }
                    return lineBuilder.ToString();
                }

                previousWasCr = (c == '\r');
                lineBuilder.Append(c);
            }
        }

        /// <summary>
        /// Serializes and writes a DAP message with Content-Length framing.
        /// Must be called within the _sendLock.
        /// </summary>
        private void SendMessageInternal(ProtocolMessage message)
        {
            var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var bodyBytes = Encoding.UTF8.GetBytes(json);
            var header = $"Content-Length: {bodyBytes.Length}\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);

            _stream.Write(headerBytes, 0, headerBytes.Length);
            _stream.Write(bodyBytes, 0, bodyBytes.Length);
            _stream.Flush();

            Trace.Verbose($"Sent: {json}");
        }

        public void SendMessage(ProtocolMessage message)
        {
            if (_stream == null)
            {
                return;
            }

            try
            {
                _sendLock.Wait();
                try
                {
                    message.Seq = _nextSeq++;
                    SendMessageInternal(message);
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception ex)
            {
                Trace.Warning($"Failed to send message: {ex.Message}");
            }
        }

        public void SendEvent(Event evt)
        {
            if (_stream == null)
            {
                Trace.Warning($"Cannot send event '{evt.EventType}': no client connected");
                return;
            }

            try
            {
                _sendLock.Wait();
                try
                {
                    evt.Seq = _nextSeq++;
                    SendMessageInternal(evt);
                }
                finally
                {
                    _sendLock.Release();
                }
                Trace.Info($"Sent event: {evt.EventType}");
            }
            catch (Exception ex)
            {
                Trace.Warning($"Failed to send event '{evt.EventType}': {ex.Message}");
            }
        }

        public void SendResponse(Response response)
        {
            if (_stream == null)
            {
                Trace.Warning($"Cannot send response for '{response.Command}': no client connected");
                return;
            }

            try
            {
                _sendLock.Wait();
                try
                {
                    response.Seq = _nextSeq++;
                    SendMessageInternal(response);
                }
                finally
                {
                    _sendLock.Release();
                }
                Trace.Info($"Sent response: seq={response.Seq}, command={response.Command}, success={response.Success}");
            }
            catch (Exception ex)
            {
                Trace.Warning($"Failed to send response for '{response.Command}': {ex.Message}");
            }
        }
    }
}
