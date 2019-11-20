using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(JobNotification))]
    public interface IJobNotification : IRunnerService, IDisposable
    {
        void JobStarted(Guid jobId, string accessToken, Uri serverUrl);
        Task JobCompleted(Guid jobId);
        void StartClient(string socketAddress, string monitorSocketAddress);
    }

    public sealed class JobNotification : RunnerService, IJobNotification
    {
        private Socket _socket;
        private Socket _monitorSocket;
        private bool _configured = false;
        private bool _isMonitorConfigured = false;

        public void JobStarted(Guid jobId, string accessToken, Uri serverUrl)
        {
            Trace.Info("Entering JobStarted Notification");

            StartMonitor(jobId, accessToken, serverUrl);

            if (_configured)
            {
                String message = $"Starting job: {jobId.ToString()}";

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
        }

        public async Task JobCompleted(Guid jobId)
        {
            Trace.Info("Entering JobCompleted Notification");

            await EndMonitor();
            
            if (_configured)
            {
                String message = $"Finished job: {jobId.ToString()}";

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
        }

        public void StartClient(string socketAddress, string monitorSocketAddress)
        {
            if (!_configured && !String.IsNullOrEmpty(socketAddress))
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
                    _configured = true;
                }
                catch (SocketException e)
                {
                    Trace.Error("Connection to socket {0} failed!", socketAddress);
                    Trace.Error(e);
                }
            }

            ConnectMonitor(monitorSocketAddress);
        }
        
        private void StartMonitor(Guid jobId, string accessToken, Uri serverUri)
        {
            if(String.IsNullOrEmpty(accessToken)) 
            {
                Trace.Info("No access token could be retrieved to start the monitor.");
                return;
            }

            try
            {
                Trace.Info("Entering StartMonitor");
                if (_isMonitorConfigured)
                {
                    String message = $"Start {jobId.ToString()} {accessToken} {serverUri.ToString()} {System.Diagnostics.Process.GetCurrentProcess().Id}";

                    Trace.Info("Writing StartMonitor to socket");
                    _monitorSocket.Send(Encoding.UTF8.GetBytes(message));
                    Trace.Info("Finished StartMonitor writing to socket");
                }
            }
            catch (SocketException e)
            {
                Trace.Error($"Failed sending StartMonitor message on socket!");
                Trace.Error(e);
            }
            catch (Exception e)
            {
                Trace.Error($"Unexpected error occurred while sending StartMonitor message on socket!");
                Trace.Error(e);
            }
        }

        private async Task EndMonitor()
        {
            try
            {
                Trace.Info("Entering EndMonitor");
                if (_isMonitorConfigured)
                {
                    String message = $"End {System.Diagnostics.Process.GetCurrentProcess().Id}";
                    Trace.Info("Writing EndMonitor to socket");
                    _monitorSocket.Send(Encoding.UTF8.GetBytes(message));
                    Trace.Info("Finished EndMonitor writing to socket");

                    await Task.Delay(TimeSpan.FromSeconds(2));                    
                }
            }
            catch (SocketException e)
            {
                Trace.Error($"Failed sending end message on socket!");
                Trace.Error(e);
            }
            catch (Exception e)
            {
                Trace.Error($"Unexpected error occurred while sending StartMonitor message on socket!");
                Trace.Error(e);
            }
        }

        private void ConnectMonitor(string monitorSocketAddress)
        {
            int port = -1;
            if (!_isMonitorConfigured && !String.IsNullOrEmpty(monitorSocketAddress))
            {
                try
                {
                    string[] splitAddress = monitorSocketAddress.Split(':');
                    if (splitAddress.Length != 2)
                    {
                        Trace.Error("Invalid socket address {0}. Unable to connect to monitor.", monitorSocketAddress);
                        return;
                    }

                    IPAddress address;
                    try
                    {
                        address = IPAddress.Parse(splitAddress[0]);
                    }
                    catch (FormatException e)
                    {
                        Trace.Error("Invalid socket IP address {0}. Unable to connect to monitor.", splitAddress[0]);
                        Trace.Error(e);
                        return;
                    }

                    Int32.TryParse(splitAddress[1], out port);
                    if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                    {
                        Trace.Error("Invalid TCP socket port {0}. Unable to connect to monitor.", splitAddress[1]);
                        return;
                    }


                    Trace.Verbose("Trying to connect to monitor at port {0}", port);
                    _monitorSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    _monitorSocket.Connect(address, port);
                    Trace.Info("Connection successful to local port {0}", port);
                    _isMonitorConfigured = true;
                }
                catch (Exception e)
                {
                    Trace.Error("Connection to monitor port {0} failed!", port);
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
                if (_socket != null)
                {
                    _socket.Send(Encoding.UTF8.GetBytes("<EOF>"));
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket = null;
                }

                if (_monitorSocket != null)
                {
                    _monitorSocket.Send(Encoding.UTF8.GetBytes("<EOF>"));
                    _monitorSocket.Shutdown(SocketShutdown.Both);
                    _monitorSocket = null;
                }
            }
        }
    }
}
