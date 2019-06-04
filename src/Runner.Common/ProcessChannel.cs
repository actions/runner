using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Common
{
    public delegate void StartProcessDelegate(string pipeHandleOut, string pipeHandleIn);

    public enum MessageType
    {
        NotInitialized = -1,
        NewJobRequest = 1,
        CancelRequest = 2,
        RunnerShutdown = 3,
        OperatingSystemShutdown = 4
    }

    public struct WorkerMessage
    {
        public MessageType MessageType;
        public string Body;
        public WorkerMessage(MessageType messageType, string body)
        {
            MessageType = messageType;
            Body = body;
        }
    }

    [ServiceLocator(Default = typeof(ProcessChannel))]
    public interface IProcessChannel : IDisposable, IRunnerService
    {
        void StartServer(StartProcessDelegate startProcess);
        void StartClient(string pipeNameInput, string pipeNameOutput);

        Task SendAsync(MessageType messageType, string body, CancellationToken cancellationToken);
        Task<WorkerMessage> ReceiveAsync(CancellationToken cancellationToken);
    }

    public sealed class ProcessChannel : RunnerService, IProcessChannel
    {
        private AnonymousPipeServerStream _inServer;
        private AnonymousPipeServerStream _outServer;
        private AnonymousPipeClientStream _inClient;
        private AnonymousPipeClientStream _outClient;
        private StreamString _writeStream;
        private StreamString _readStream;

        public void StartServer(StartProcessDelegate startProcess)
        {
            _outServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _inServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            _readStream = new StreamString(_inServer);
            _writeStream = new StreamString(_outServer);
            startProcess(_outServer.GetClientHandleAsString(), _inServer.GetClientHandleAsString());
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

        public async Task SendAsync(MessageType messageType, string body, CancellationToken cancellationToken)
        {
            await _writeStream.WriteInt32Async((int)messageType, cancellationToken);
            await _writeStream.WriteStringAsync(body, cancellationToken);
        }

        public async Task<WorkerMessage> ReceiveAsync(CancellationToken cancellationToken)
        {
            WorkerMessage result = new WorkerMessage(MessageType.NotInitialized, string.Empty);
            result.MessageType = (MessageType)await _readStream.ReadInt32Async(cancellationToken);
            result.Body = await _readStream.ReadStringAsync(cancellationToken);
            return result;
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
                _inServer?.Dispose();
                _outServer?.Dispose();
                _inClient?.Dispose();
                _outClient?.Dispose();
            }
        }
    }
}
