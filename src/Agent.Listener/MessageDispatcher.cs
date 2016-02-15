using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(MessageDispatcher))]
    public interface IMessageDispatcher
    {
        void Dispatch(TaskAgentMessage message);
    }

    public sealed class MessageDispatcher : IMessageDispatcher
    {
        // AgentRefreshMessage.MessageType
        // JobCancelMessage.MessageType
        // JobRequestMessage.MessageType
        public void Dispatch(TaskAgentMessage message)
        {
            throw new System.NotImplementedException();
        }
   }
}