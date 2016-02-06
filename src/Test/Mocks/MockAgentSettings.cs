using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MockAgentSettings : IAgentSettings
    {
        public Int32 AgentId { get; set; }
        public String AgentName { get; set; }
        public Boolean AutoUpdate { get; set; }
        public Int32 PoolId { get; set; }
        public String PoolName { get; set; }
        public String RootFolder { get; set; }
        public Boolean RunAsWindowsService { get; set; }
        public String ServerUrl { get; set; }
        public String WorkFolder { get; set; }

        public Task SaveAsync()
        {
            throw new System.NotSupportedException();
        }
    }
}
