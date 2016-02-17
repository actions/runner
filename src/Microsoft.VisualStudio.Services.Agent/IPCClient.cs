
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class IPCClient : IDisposable
    {
        private Task RunTask;
        private CancellationTokenSource TokenSource;
        private AnonymousPipeClientStream InClient;
        private AnonymousPipeClientStream OutClient;
        private volatile bool Started = false;

        public StreamTransport Transport { get; set; }

        public IPCClient()
        {
            Transport = new StreamTransport();
            TokenSource = new CancellationTokenSource();
        }

        public async Task Start(string pipeNameInput, string pipeNameOutput)
        {
            Debug.Assert(!disposedValue);
            if (!Started)
            {
                Started = true;
                InClient = new AnonymousPipeClientStream(PipeDirection.In, pipeNameInput);
                OutClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeNameOutput);
                Transport.ReadPipe = InClient;
                Transport.WritePipe = OutClient;
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
                    Console.WriteLine("Worker exception: {0}", ex.ToString());
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
                if (null != InClient)
                {
                    InClient.Dispose();
                    InClient = null;
                }
                if (null != OutClient)
                {
                    OutClient.Dispose();
                    OutClient = null;
                }
                TokenSource.Dispose();

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
