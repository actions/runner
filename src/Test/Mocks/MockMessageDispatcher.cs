using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MockMessageDispatcher : IMessageDispatcher
    {
        public Action<TaskAgentMessage> _Dispatch { get; set; }

        public void Dispatch(TaskAgentMessage message)
        {
            if (this._Dispatch != null) { this._Dispatch(message); }
        }
    }
}
