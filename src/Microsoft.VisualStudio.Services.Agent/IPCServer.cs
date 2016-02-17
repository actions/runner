using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class IPCServer : IDisposable
    {
        private Task RunTask;
        private CancellationTokenSource TokenSource;
        private AnonymousPipeServerStream InServer;
        private AnonymousPipeServerStream OutServer;
        private volatile bool Started = false;
        private System.Diagnostics.Process ClientProcess;

        public StreamTransport Transport { get; set; }        

        public IPCServer()
        {
            Transport = new StreamTransport();
            TokenSource = new CancellationTokenSource();
        }

        public void Start(String clientFileName)
        {
            Debug.Assert(!disposedValue);
            if (!Started)
            {
                Started = true;
                OutServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                InServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
                Transport.ReadPipe = InServer;
                Transport.WritePipe = OutServer;
                try {
                    //TODO: ise ProcessInvoker
                    ClientProcess = System.Diagnostics.Process.Start(clientFileName, "spawnclient " +
                        OutServer.GetClientHandleAsString() + " " + InServer.GetClientHandleAsString());
                }
                catch (Exception ex)
                {
                    //TODO: write error in the log file
                    Console.WriteLine("failed to start {0}", ex.ToString());
                }
                OutServer.DisposeLocalCopyOfClientHandle();
                InServer.DisposeLocalCopyOfClientHandle();
                RunTask = Transport.Run(TokenSource.Token);
            }
        }

        public async Task Stop()
        {
            Debug.Assert(!disposedValue);

            if (Started)
            {
                Started = false;
                TokenSource.Cancel();

                try
                {
                    await RunTask;
                }
                catch (TaskCanceledException)
                {
                    // Ignore this exception
                }
                catch (Exception ex)
                {
                    //TODO: log the exception in the context
                    Console.WriteLine("Server exception: {0}", ex.ToString());
                }

                while (!ClientProcess.HasExited)
                {
                    await Task.Delay(100);
                }

                Dispose(true);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (null != OutServer)
                {
                    OutServer.Dispose();
                    OutServer = null;
                }
                if (null != InServer)
                {
                    InServer.Dispose();
                    InServer = null;
                }
                TokenSource.Dispose();
                ClientProcess.Dispose();

                disposedValue = true;
            }
        }        

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }

}
