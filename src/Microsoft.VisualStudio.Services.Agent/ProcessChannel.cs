using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ProcessChannel))]
    public interface IProcessChannel : IDisposable, IAgentService
    {
        event Func<JobRequestMessage, CancellationToken, Task> JobRequestMessageReceived;
        event Func<JobCancelMessage, CancellationToken, Task> JobCancelMessageReceived;

        Task SendAsync(JobRequestMessage jobRequest, CancellationToken cancellationToken);
        Task SendAsync(JobCancelMessage jobCancel, CancellationToken cancellationToken);
        void StartServer(ProcessStartDelegate processStart);
        void StartClient(string pipeNameInput, string pipeNameOutput);
        Task Stop();
    }

    public delegate void ProcessStartDelegate(String pipeHandleOut, String pipeHandleIn);

    public sealed class ProcessChannel : AgentService, IProcessChannel
    {
        private Task RunTask;
        private CancellationTokenSource TokenSource;
        private AnonymousPipeServerStream InServer;
        private AnonymousPipeServerStream OutServer;
        private AnonymousPipeClientStream InClient;
        private AnonymousPipeClientStream OutClient;
        private volatile bool Started = false;
        private StreamTransport Transport { get; set; }

        public ProcessChannel()
        {
            Transport = new StreamTransport();
            TokenSource = new CancellationTokenSource();
        }

        public void StartServer(ProcessStartDelegate processStart)
        {
            if (!Started)
            {
                Started = true;
                OutServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                InServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
                Transport.ReadPipe = InServer;
                Transport.WritePipe = OutServer;
                processStart(OutServer.GetClientHandleAsString(), InServer.GetClientHandleAsString());
                OutServer.DisposeLocalCopyOfClientHandle();
                InServer.DisposeLocalCopyOfClientHandle();
                Transport.PacketReceived += Transport_PacketReceived;
                RunTask = Transport.Run(TokenSource.Token);
            }
        }

        public void StartClient(string pipeNameInput, string pipeNameOutput)
        {
            if (!Started)
            {
                Started = true;
                InClient = new AnonymousPipeClientStream(PipeDirection.In, pipeNameInput);
                OutClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeNameOutput);
                Transport.ReadPipe = InClient;
                Transport.WritePipe = OutClient;
                Transport.PacketReceived += Transport_PacketReceived;
                RunTask = Transport.Run(TokenSource.Token);
            }
        }

        public async Task Stop()
        {
            if (Started)
            {
                Started = false;
                Transport.PacketReceived -= Transport_PacketReceived;
                TokenSource.Cancel();

                try
                {
                    await RunTask;
                }
                catch (OperationCanceledException)
                {
                    // Ignore OperationCanceledException and TaskCanceledException exceptions
                }
                catch (AggregateException errors)
                {
                    // Ignore OperationCanceledException and TaskCanceledException exceptions
                    errors.Handle(e => e is OperationCanceledException);
                }
            }
        }

        private async Task Transport_PacketReceived(IPCPacket packet, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            switch (packet.MessageType)
            {
                case 1:
                    {                        
                        var message = JsonUtility.FromString<JobRequestMessage>(packet.Body);
                        Func<JobRequestMessage, CancellationToken, Task> jobRequestMessageReceived = JobRequestMessageReceived;
                        if (null == jobRequestMessageReceived)
                        {
                            return;
                        }
                        Delegate[] invocationList = jobRequestMessageReceived.GetInvocationList();
                        Task[] handlerTasks = new Task[invocationList.Length];
                        for (int i = 0; i < invocationList.Length; i++)
                        {
                            handlerTasks[i] = ((Func<JobRequestMessage, CancellationToken, Task>)invocationList[i])(message, token);
                        }
                        await Task.WhenAll(handlerTasks);
                    }
                    break;
                case 2:
                    {
                        var message = JsonUtility.FromString<JobCancelMessage>(packet.Body);
                        Func<JobCancelMessage, CancellationToken, Task> jobCancelMessageReceived = JobCancelMessageReceived;
                        if (null == jobCancelMessageReceived)
                        {
                            return;
                        }
                        Delegate[] invocationList = jobCancelMessageReceived.GetInvocationList();
                        Task[] handlerTasks = new Task[invocationList.Length];
                        for (int i = 0; i < invocationList.Length; i++)
                        {
                            handlerTasks[i] = ((Func<JobCancelMessage, CancellationToken, Task>)invocationList[i])(message, token);
                        }
                        await Task.WhenAll(handlerTasks);
                    }
                    break;
            }
        }

        public event Func<JobRequestMessage, CancellationToken, Task> JobRequestMessageReceived;
        public event Func<JobCancelMessage, CancellationToken, Task> JobCancelMessageReceived;

        public async Task SendAsync(JobRequestMessage jobRequest, CancellationToken cancellationToken)
        {
            string messageString = JsonUtility.ToString(jobRequest);
            await Transport.SendAsync(1, messageString, cancellationToken);
        }

        public async Task SendAsync(JobCancelMessage jobCancel, CancellationToken cancellationToken)
        {
            string messageString = JsonUtility.ToString(jobCancel);
            await Transport.SendAsync(2, messageString, cancellationToken);
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
                TokenSource.Dispose();
                InServer?.Dispose();
                OutServer?.Dispose();
                InClient?.Dispose();
                OutClient?.Dispose();
            }
        }
    }
}
