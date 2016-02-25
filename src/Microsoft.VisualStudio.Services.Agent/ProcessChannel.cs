using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public struct IPCPacket
    {
        public Int32 _messageType;
        public string _body;
        public IPCPacket(Int32 p1, string p2)
        {
            _messageType = p1;
            _body = p2;
        }
    }

    [ServiceLocator(Default = typeof(ProcessChannel))]
    public interface IProcessChannel : IDisposable, IAgentService
    {
        void StartServer(ProcessStartDelegate processStart);
        void StartClient(string pipeNameInput, string pipeNameOutput);

        Task SendAsync(Int32 MessageType, string Body, CancellationToken cancellationToken);
        Task<IPCPacket> ReceiveAsync(CancellationToken cancellationToken);
    }

    public delegate void ProcessStartDelegate(String pipeHandleOut, String pipeHandleIn);

    public class ProcessChannel : AgentService, IProcessChannel
    {
        private AnonymousPipeServerStream _inServer;
        private AnonymousPipeServerStream _outServer;
        private AnonymousPipeClientStream _inClient;
        private AnonymousPipeClientStream _outClient;
        private StreamString _writeStream;
        private StreamString _readStream;

        public void StartServer(ProcessStartDelegate processStart)
        {
            _outServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _inServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            _readStream = new StreamString(_inServer);
            _writeStream = new StreamString(_outServer);
            processStart(_outServer.GetClientHandleAsString(), _inServer.GetClientHandleAsString());
            _outServer.DisposeLocalCopyOfClientHandle();
            _inServer.DisposeLocalCopyOfClientHandle();
        }

        public void StartClient(string pipeNameInput, string pipeNameOutput)
        {
            _inClient = new AnonymousPipeClientStream(PipeDirection.In, pipeNameInput);
            _outClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeNameOutput);
            _readStream = new StreamString(_inClient);
            _writeStream = new StreamString(_outClient);
        }
                
        public async Task SendAsync(Int32 MessageType, string Body, CancellationToken cancellationToken)
        {
            await _writeStream.WriteInt32Async(MessageType, cancellationToken);
            await _writeStream.WriteStringAsync(Body, cancellationToken);
        }

        public async Task<IPCPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            IPCPacket result = new IPCPacket(-1, "");
            result._messageType = await _readStream.ReadInt32Async(cancellationToken);
            result._body = await _readStream.ReadStringAsync(cancellationToken);
            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (null != _inServer) _inServer.Dispose();
                if (null != _outServer) _outServer.Dispose();
                if (null != _inClient) _inClient.Dispose();
                if (null != _outClient) _outClient.Dispose();
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
