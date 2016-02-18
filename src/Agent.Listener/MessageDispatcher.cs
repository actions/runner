using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(MessageDispatcher))]
    public interface IMessageDispatcher : IDisposable
    {
        Task Dispatch(IHostContext context, TaskAgentMessage message);
    }

    public sealed class MessageDispatcher : IMessageDispatcher
    {
        private IPCServer Server;

        public MessageDispatcher()
        {
        }

        private async Task Transport_PacketReceived(object sender, IPCPacket e)
        {
            Console.WriteLine("Agent received: {0} {1}", e.MessageType, e.Body);
        }


        // AgentRefreshMessage.MessageType
        // JobCancelMessage.MessageType
        // JobRequestMessage.MessageType
        public async Task Dispatch(IHostContext context, TaskAgentMessage message)
        {
            //TODO: IPCServer probably needs to stay alive for a few messages
            using (Server = new IPCServer())
            {
                Server.Transport.PacketReceived += Transport_PacketReceived;
                Server.Start("Agent.Worker");

                string messageString = JsonUtility.ToString(message);                
                await Server.Transport.SendAsync(1, messageString, context.CancellationToken);

                Server.Transport.PacketReceived -= Transport_PacketReceived;
                await Server.Stop();
            }
            Server = null;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (null != Server)
                {
                    Server.Transport.PacketReceived -= Transport_PacketReceived;
                    Server.Stop().Wait();
                    Server.Dispose();
                }
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