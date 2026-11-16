using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.DistributedTask.Logging;
using GitHub.Runner.Sdk;
using Newtonsoft.Json;

namespace GitHub.Runner.Common
{

    [ServiceLocator(Default = typeof(VSockSecretNotifier))]
    public interface IVSockSecretNotifier : IRunnerService, IAsyncDisposable
    {
        bool TryStartNotifier();

        void NotifyNewSecret(NewSecretEventArgs newSecret);
    }

    public sealed class VSockSecretNotifier : RunnerService, IVSockSecretNotifier
    {
        private Socket _vsock = null;

        private CancellationTokenSource _cancellationTokenSource = null;

        private Task _secretNotificationTask = null;

        private Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions() { SingleReader = true });

        public bool TryStartNotifier()
        {
            if (_vsock != null)
            {
                Trace.Verbose("VSocket is already connected.");
                return true;
            }

            // `GITHUB_ACTIONS_RUNNER_VSOCK_CID_PORT` is expected to be in the format "CID:PORT", e.g. "2:9999".
            string vsockCidPort = Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_VSOCK_CID_PORT");
            if (string.IsNullOrEmpty(vsockCidPort))
            {
                Trace.Verbose("VSocket CID/Port environment variable is not set.");
                return false;
            }

            string[] parts = vsockCidPort.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                Trace.Verbose("VSocket CID/Port environment variable is not in the correct format.");
                return false;
            }

            uint cid, port;
            if (!uint.TryParse(parts[0], out cid) || !uint.TryParse(parts[1], out port))
            {
                Trace.Verbose("VSocket CID/Port environment variable contains invalid numbers.");
                return false;
            }

            Trace.Info($"Attempting to start VSocket secret notifier with CID: {cid}, Port: {port}.");
            try
            {
                SafeSocketHandle nativeSocket = NativeSocket((int)(AddressFamily)40, (int)SocketType.Stream, 0);
                if (nativeSocket.IsInvalid)
                {
                    int error = Marshal.GetLastPInvokeError();
                    nativeSocket.Dispose();
                    throw new SocketException(error);
                }

                _vsock = new Socket(nativeSocket);
                _vsock.Connect(new HostVsockEndPoint(cid, port));
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to create and connect VSocket: {ex}");
                _vsock?.Dispose();
                _vsock = null;
                return false;
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(HostContext.RunnerShutdownToken);
            _secretNotificationTask = ProcessSecretChannel();
            Trace.Info($"VSocket secret notifier started successfully.");
            return true;
        }

        public void NotifyNewSecret(NewSecretEventArgs newSecret)
        {
            if (_vsock == null)
            {
                Trace.Verbose("VSocket is not connected, skipping secret notification.");
                return;
            }

            byte[] payloadBytes = Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(new { RunnerSecrets = newSecret }, Formatting.None));
            byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payloadBytes.Length));
            byte[] fullPayload = new byte[lengthPrefix.Length + payloadBytes.Length];
            Buffer.BlockCopy(lengthPrefix, 0, fullPayload, 0, lengthPrefix.Length);
            Buffer.BlockCopy(payloadBytes, 0, fullPayload, lengthPrefix.Length, payloadBytes.Length);

            // we don't need to check return since unbounded channel will always accept the item.
            _channel.Writer.TryWrite(fullPayload);
        }

        public async ValueTask DisposeAsync()
        {
            if (_vsock != null && _secretNotificationTask != null)
            {
                _cancellationTokenSource?.Cancel();
                try
                {
                    await _secretNotificationTask;
                }
                catch (Exception ex)
                {
                    Trace.Error($"Secret notification task finished with error: {ex}");
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _vsock?.Dispose();
                _vsock = null;
            }
        }

        private async Task ProcessSecretChannel()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested &&
                        await _channel.Reader.WaitToReadAsync(_cancellationTokenSource.Token))
                {
                    while (_channel.Reader.TryRead(out var payload))
                    {
                        try
                        {
                            // Socket.SendAsync on a stream socket may send fewer bytes than requested,
                            // so keep sending until the entire payload has been written.
                            int totalSent = 0;
                            while (totalSent < payload.Length)
                            {
                                int bytesSent = await _vsock.SendAsync(payload.AsMemory(totalSent), SocketFlags.None, _cancellationTokenSource.Token);
                                if (bytesSent == 0)
                                {
                                    throw new SocketException((int)SocketError.ConnectionReset);
                                }

                                totalSent += bytesSent;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Trace.Info("Secret notification task was canceled.");
                        }
                        catch (Exception ex)
                        {
                            Trace.Error($"Failed to notify new secret over VSocket: {ex}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Trace.Info("Secret notification task was canceled.");
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to process secret channel: {ex}");
            }

            _channel.Writer.TryComplete();
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "socket")]
        private static extern SafeSocketHandle NativeSocket(int domain, int type, int protocol);
    }

    internal sealed class HostVsockEndPoint : EndPoint
    {
        private const int SocketAddressSize = 16;
        private readonly uint _cid;
        private readonly uint _port;

        public HostVsockEndPoint(uint cid, uint port)
        {
            _cid = cid;
            _port = port;
        }

        public override AddressFamily AddressFamily => (AddressFamily)40;

        public override SocketAddress Serialize()
        {
            SocketAddress socketAddress = new SocketAddress(AddressFamily.Unspecified, SocketAddressSize);
            // sockaddr_vm layout: family(0-1), reserved1(2-3), port(4-7), cid(8-11)
            ushort family = (ushort)AddressFamily;
            socketAddress[0] = (byte)(family & 0xFF);
            socketAddress[1] = (byte)((family >> 8) & 0xFF);
            socketAddress[2] = 0;
            socketAddress[3] = 0;
            socketAddress[4] = (byte)(_port & 0xFF);
            socketAddress[5] = (byte)((_port >> 8) & 0xFF);
            socketAddress[6] = (byte)((_port >> 16) & 0xFF);
            socketAddress[7] = (byte)((_port >> 24) & 0xFF);
            socketAddress[8] = (byte)(_cid & 0xFF);
            socketAddress[9] = (byte)((_cid >> 8) & 0xFF);
            socketAddress[10] = (byte)((_cid >> 16) & 0xFF);
            socketAddress[11] = (byte)((_cid >> 24) & 0xFF);
            return socketAddress;
        }

        public override EndPoint Create(SocketAddress socketAddress)
        {
            uint port = (uint)socketAddress[4]
                | ((uint)socketAddress[5] << 8)
                | ((uint)socketAddress[6] << 16)
                | ((uint)socketAddress[7] << 24);

            uint cid = (uint)socketAddress[8]
                | ((uint)socketAddress[9] << 8)
                | ((uint)socketAddress[10] << 16)
                | ((uint)socketAddress[11] << 24);

            return new HostVsockEndPoint(cid, port);
        }
    }
}
