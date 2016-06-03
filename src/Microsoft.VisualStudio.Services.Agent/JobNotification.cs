using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.IO.Pipes;
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
    }

    public sealed class JobNotification : AgentService, IJobNotification
    {
        private NamedPipeClientStream _outClient;
        private StreamWriter _writeStream;
        private bool _configured = false;

        public async Task JobStarted(Guid jobId)
        {
            Trace.Info("Entering JobStarted Notification");
            if (_configured)
            {
                Trace.Info("Writing JobStarted to pipe");
                await _writeStream.WriteLineAsync($"Starting job: {jobId.ToString()}");
                await _writeStream.FlushAsync();
                Trace.Info("Finished JobStarted writing to pipe");
            }
        }

        public async Task JobCompleted(Guid jobId)
        {
            Trace.Info("Entering JobCompleted Notification");
            if (_configured)
            {
                Trace.Info("Writing JobCompleted to pipe");
                await _writeStream.WriteLineAsync($"Finished job: {jobId.ToString()}" );
                await _writeStream.FlushAsync();
                Trace.Info("Finished JobCompleted writing to pipe");
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
                Trace.Info("Connection successfull to named pipe {0}", pipeName);
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
            }
        }
    }
}