using System;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace GitHub.Runner.Common
{
    public sealed class RunnerFirewallNotifier
    {
        private const AddressFamily VsockAddressFamily = (AddressFamily)40;
        internal bool IsLinux { get; }
        private readonly Socket _vsock;
        private readonly object _vsockSendLock = new object();

        public RunnerFirewallNotifier()
        {
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            _vsock = IsLinux ? TryCreateConnectedVsock() : null;
        }

        public void NotifySecretRegistration(List<string> secrets, List<string> secretRegexes)
        {
            if (_vsock == null)
            {
                return;
            }

            try
            {
                string jsonPayload = JsonSerializer.Serialize(new
                {
                    RunnerSecrets = new
                    {
                        secrets = secrets ?? new List<string>(),
                        secretRegexes = secretRegexes ?? new List<string>()
                    }
                });

                byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
                byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payloadBytes.Length));

                lock (_vsockSendLock)
                {
                    SendAll(_vsock, lengthPrefix);
                    SendAll(_vsock, payloadBytes);
                }
            }
            catch
            {
                // Notification delivery is best-effort
            }
        }

        private static Socket TryCreateConnectedVsock()
        {
            Socket socket = null;
            try
            {
                SafeSocketHandle nativeSocket = NativeSocket((int)VsockAddressFamily, (int)SocketType.Stream, 0);
                if (nativeSocket.IsInvalid)
                {
                    int error = Marshal.GetLastPInvokeError();
                    nativeSocket.Dispose();
                    throw new SocketException(error);
                }

                socket = new Socket(nativeSocket);
                socket.Connect(new HostVsockEndPoint(2, 9999));
                return socket;
            }
            catch
            {
                socket?.Dispose();
                return null;
            }
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "socket")]
        private static extern SafeSocketHandle NativeSocket(int domain, int type, int protocol);

        private static void SendAll(Socket socket, byte[] buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int bytesSent = socket.Send(buffer, offset, buffer.Length - offset, SocketFlags.None);
                if (bytesSent <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }

                offset += bytesSent;
            }
        }

        private sealed class HostVsockEndPoint : EndPoint
        {
            private const int SocketAddressSize = 16;
            private readonly uint _cid;
            private readonly uint _port;

            public HostVsockEndPoint(uint cid, uint port)
            {
                _cid = cid;
                _port = port;
            }

            public override AddressFamily AddressFamily => VsockAddressFamily;

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
}
