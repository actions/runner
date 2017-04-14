using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(JobNotification))]
    public interface IJobNotification : IAgentService, IDisposable
    {
        Task JobStarted(Guid jobId);
        Task JobCompleted(Guid jobId);
        void StartClient(string pipeName, CancellationToken cancellationToken);
        void StartClient(string socketAddress);
    }

    public sealed class JobNotification : AgentService, IJobNotification
    {
        private NamedPipeClientStream _outClient;
        private StreamWriter _writeStream;
        private Socket _socket;
        private bool _configured = false;
        private bool _useSockets = false;

        public async Task JobStarted(Guid jobId)
        {
            Trace.Info("Entering JobStarted Notification");
            if (_configured)
            {
                String message = $"Starting job: {jobId.ToString()}";
                if (_useSockets)
                {
                    try
                    {
                        Trace.Info("Writing JobStarted to socket");
                        _socket.Send(Encoding.UTF8.GetBytes(message));
                        Trace.Info("Finished JobStarted writing to socket");
                    }
                    catch (SocketException e)
                    {
                        Trace.Error($"Failed sending message \"{message}\" on socket!");
                        Trace.Error(e);
                    }
                }
                else
                {
                    Trace.Info("Writing JobStarted to pipe");
                    await _writeStream.WriteLineAsync(message);
                    await _writeStream.FlushAsync();
                    Trace.Info("Finished JobStarted writing to pipe");  
                }
            }
        }

        public async Task JobCompleted(Guid jobId)
        {
            Trace.Info("Entering JobCompleted Notification");
            if (_configured)
            {
                String message = $"Finished job: {jobId.ToString()}";
                if (_useSockets)
                {
                    try
                    {
                        Trace.Info("Writing JobCompleted to socket");
                        _socket.Send(Encoding.UTF8.GetBytes(message));
                        Trace.Info("Finished JobCompleted writing to socket");
                    }
                    catch (SocketException e)
                    {
                        Trace.Error($"Failed sending message \"{message}\" on socket!");
                        Trace.Error(e);
                    }
                }
                else
                {
                    Trace.Info("Writing JobCompleted to pipe");
                    await _writeStream.WriteLineAsync(message);
                    await _writeStream.FlushAsync();
                    Trace.Info("Finished JobCompleted writing to pipe");
                }
            }
        }

        public async void StartClient(string pipeName, CancellationToken cancellationToken)
        {
            if (pipeName != null && !_configured)
            {
                Trace.Info("Connecting to named pipe {0}", pipeName);
                _outClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                await _outClient.ConnectAsync(cancellationToken);
                _writeStream = new StreamWriter(_outClient, Encoding.UTF8);
                _configured = true;
                Trace.Info("Connection successful to named pipe {0}", pipeName);
            }
        }

        public void StartClient(string socketAddress)
        {
            if (!_configured)
            {
                try
                {
                    string[] splitAddress = socketAddress.Split(':');
                    if (splitAddress.Length != 2)
                    {
                        Trace.Error("Invalid socket address {0}. Job Notification will be disabled.", socketAddress);
                        return;
                    }

                    IPAddress address;
                    try
                    {
                        address = IPAddress.Parse(splitAddress[0]);
                    }
                    catch (FormatException e)
                    {
                        Trace.Error("Invalid socket ip address {0}. Job Notification will be disabled",splitAddress[0]);
                        Trace.Error(e);
                        return;
                    }

                    int port = -1;
                    Int32.TryParse(splitAddress[1], out port);
                    if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                    {
                        Trace.Error("Invalid tcp socket port {0}. Job Notification will be disabled.", splitAddress[1]);
                        return;
                    }

                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(address, port);
                    Trace.Info("Connection successful to socket {0}", socketAddress);
                    _useSockets = true;
                    _configured = true;
                }
                catch (SocketException e)
                {
                    Trace.Error("Connection to socket {0} failed!", socketAddress);
                    Trace.Error(e);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outClient?.Dispose();

                if (_socket != null)
                {
                    _socket.Send(Encoding.UTF8.GetBytes("<EOF>"));
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket = null;
                }
            }
        }
    }
}