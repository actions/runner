using System;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MockMessageDispatcher : IMessageDispatcher
    {
        public Action<AgentMessage> _Dispatch { get; set; }

        public void Dispatch(AgentMessage message)
        {
            if (this._Dispatch != null) { this._Dispatch(message); }
        }
    }
}
