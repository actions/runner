using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public struct IPCPacket
    {
        public Int32 MessageType;
        public string Body;
        public IPCPacket(Int32 p1, string p2)
        {
            MessageType = p1;
            Body = p2;
        }
    }

    public class StreamTransport
    {
        public event Func<IPCPacket, CancellationToken, Task> PacketReceived;

        public Stream ReadPipe
        {
            set
            {
                ReadStream = new StreamString(value);
            }
        }
        public Stream WritePipe
        {
            set
            {
                WriteStream = new StreamString(value);
            }
        }

        private StreamString WriteStream { get; set; }

        private StreamString ReadStream { get; set; }

        public async Task SendAsync(Int32 MessageType, string Body, CancellationToken cancellationToken)
        {
            await WriteStream.WriteInt32Async(MessageType, cancellationToken);
            await WriteStream.WriteStringAsync(Body, cancellationToken);
        }

        public async Task<IPCPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            IPCPacket result = new IPCPacket(-1, "");
            result.MessageType = await ReadStream.ReadInt32Async(cancellationToken);
            result.Body = await ReadStream.ReadStringAsync(cancellationToken);
            return result;
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var packet = await ReceiveAsync(token);
                Func<IPCPacket, CancellationToken, Task> packetReceived = PacketReceived;
                if (null == packetReceived)
                {
                    continue;
                }
                Delegate[] invocationList = packetReceived.GetInvocationList();
                Task[] handlerTasks = new Task[invocationList.Length];
                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ((Func<IPCPacket, CancellationToken, Task>)invocationList[i])(packet, token);
                }
                await Task.WhenAll(handlerTasks);
            }
        }
    }
}
