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
    /// DAP Server interface for handling Debug Adapter Protocol connections.
    /// </summary>
    [ServiceLocator(Default = typeof(DapServer))]
    public interface IDapServer : IRunnerService, IDisposable
    {
        /// <summary>
        /// Starts the DAP TCP server on the specified port.
        /// </summary>
        /// <param name="port">The port to listen on (default: 4711)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAsync(int port, CancellationToken cancellationToken);

        /// <summary>
        /// Blocks until a debug client connects.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task WaitForConnectionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the DAP server and closes all connections.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Sets the debug session that will handle DAP requests.
        /// </summary>
        /// <param name="session">The debug session</param>
        void SetSession(IDapDebugSession session);

        /// <summary>
        /// Sends an event to the connected debug client.
        /// </summary>
        /// <param name="evt">The event to send</param>
        void SendEvent(Event evt);

        /// <summary>
        /// Gets whether a debug client is currently connected.
        /// </summary>
        bool IsConnected { get; }
    }

    /// <summary>
    /// TCP server implementation of the Debug Adapter Protocol.
    /// Handles message framing (Content-Length headers) and JSON serialization.
    /// </summary>
    public sealed class DapServer : RunnerService, IDapServer
    {
        private const string ContentLengthHeader = "Content-Length: ";
        private const string HeaderTerminator = "\r\n\r\n";

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private IDapDebugSession _session;
        private CancellationTokenSource _cts;
        private Task _messageLoopTask;
        private TaskCompletionSource<bool> _connectionTcs;
        private int _nextSeq = 1;
        private readonly object _sendLock = new object();
        private bool _disposed = false;

        public bool IsConnected => _client?.Connected == true;

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

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                Trace.Info($"DAP server listening on 127.0.0.1:{port}");

                // Start accepting connections in the background
                _ = AcceptConnectionAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to start DAP server: {ex.Message}");
                throw;
            }

            await Task.CompletedTask;
        }

        private async Task AcceptConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                Trace.Info("Waiting for debug client connection...");

                // Use cancellation-aware accept
                using (cancellationToken.Register(() => _listener?.Stop()))
                {
                    _client = await _listener.AcceptTcpClientAsync();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _stream = _client.GetStream();
                var remoteEndPoint = _client.Client.RemoteEndPoint;
                Trace.Info($"Debug client connected from {remoteEndPoint}");

                // Signal that connection is established
                _connectionTcs.TrySetResult(true);

                // Start processing messages
                _messageLoopTask = ProcessMessagesAsync(_cts.Token);
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when cancellation stops the listener
                Trace.Info("Connection accept cancelled");
                _connectionTcs.TrySetCanceled();
            }
            catch (SocketException ex) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when cancellation stops the listener
                Trace.Info($"Connection accept cancelled: {ex.Message}");
                _connectionTcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                Trace.Error($"Error accepting connection: {ex.Message}");
                _connectionTcs.TrySetException(ex);
            }
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

            _cts?.Cancel();

            // Wait for message loop to complete
            if (_messageLoopTask != null)
            {
                try
                {
                    await _messageLoopTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    Trace.Warning($"Message loop ended with error: {ex.Message}");
                }
            }

            // Clean up resources
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();

            Trace.Info("DAP server stopped");
        }

        public void SendEvent(Event evt)
        {
            if (!IsConnected)
            {
                Trace.Warning($"Cannot send event '{evt.EventType}': no client connected");
                return;
            }

            try
            {
                lock (_sendLock)
                {
                    evt.Seq = _nextSeq++;
                    SendMessageInternal(evt);
                }
                Trace.Info($"Sent event: {evt.EventType}");
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to send event '{evt.EventType}': {ex.Message}");
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            Trace.Info("Starting DAP message processing loop");

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    var json = await ReadMessageAsync(cancellationToken);
                    if (json == null)
                    {
                        Trace.Info("Client disconnected (end of stream)");
                        break;
                    }

                    await ProcessMessageAsync(json, cancellationToken);
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

        private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
        {
            Request request = null;
            try
            {
                // Parse the incoming message
                request = JsonConvert.DeserializeObject<Request>(json);
                if (request == null || request.Type != "request")
                {
                    Trace.Warning($"Received non-request message: {json}");
                    return;
                }

                Trace.Info($"Received request: seq={request.Seq}, command={request.Command}");

                // Dispatch to session for handling
                if (_session == null)
                {
                    Trace.Error("No debug session configured");
                    SendErrorResponse(request, "No debug session configured");
                    return;
                }

                var response = await _session.HandleRequestAsync(request);
                response.RequestSeq = request.Seq;
                response.Command = request.Command;
                response.Type = "response";

                lock (_sendLock)
                {
                    response.Seq = _nextSeq++;
                    SendMessageInternal(response);
                }

                Trace.Info($"Sent response: seq={response.Seq}, command={response.Command}, success={response.Success}");
            }
            catch (JsonException ex)
            {
                Trace.Error($"Failed to parse request: {ex.Message}");
                Trace.Error($"JSON: {json}");
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

            lock (_sendLock)
            {
                response.Seq = _nextSeq++;
                SendMessageInternal(response);
            }
        }

        /// <summary>
        /// Reads a DAP message from the stream.
        /// DAP uses HTTP-like message framing: Content-Length: N\r\n\r\n{json}
        /// </summary>
        private async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            // Read headers until we find Content-Length
            var headerBuilder = new StringBuilder();
            int contentLength = -1;

            while (true)
            {
                var line = await ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    // End of stream
                    return null;
                }

                if (line.Length == 0)
                {
                    // Empty line marks end of headers
                    break;
                }

                headerBuilder.AppendLine(line);

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

            // Read the JSON body
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
        /// Reads a line from the stream (terminated by \r\n).
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
                    // End of stream
                    return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
                }

                var c = (char)buffer[0];

                if (c == '\n' && previousWasCr)
                {
                    // Found \r\n, return the line (without the \r)
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
        /// Sends a DAP message to the stream with Content-Length framing.
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
            var headerBytes = Encoding.UTF8.GetBytes(header);

            _stream.Write(headerBytes, 0, headerBytes.Length);
            _stream.Write(bodyBytes, 0, bodyBytes.Length);
            _stream.Flush();

            Trace.Verbose($"Sent: {json}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cts?.Cancel();
                _stream?.Dispose();
                _client?.Dispose();
                _listener?.Stop();
                _cts?.Dispose();
            }

            _disposed = true;
        }
    }
}
