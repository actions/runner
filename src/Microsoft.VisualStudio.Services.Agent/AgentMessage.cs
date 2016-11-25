using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class AgentMessage
    {
        public AgentJobRequestMessage JobRequest { get; set; }
        public Boolean CanRaiseJobCompletedEvent { get; set; }
    }
}
