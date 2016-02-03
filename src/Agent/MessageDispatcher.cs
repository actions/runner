using System;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public interface IMessageDispatcher
    {
        void Dispatch(AgentMessage message);
    }

    public sealed class MessageDispatcher : IMessageDispatcher
    {
        public void Dispatch(AgentMessage message)
        {
            throw new System.NotImplementedException();
        }
   }
}