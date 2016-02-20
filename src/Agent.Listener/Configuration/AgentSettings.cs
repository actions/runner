using System;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    [ServiceLocator(Default = typeof(AgentSettings))]
    public interface IAgentSettings
    {
        Int32 AgentId { get; }

        String AgentName { get; }

        Boolean AutoUpdate { get; }

        Int32 PoolId { get; }

        String PoolName { get; }

        String RootFolder { get; }

        Boolean RunAsService { get; }

        String ServerUrl { get; }

        String WorkFolder { get; }
    }

    public sealed class AgentSettings : IAgentSettings
    {
        public Int32 AgentId { get; set; }

        public String AgentName { get; set; }

        public Boolean AutoUpdate { get; set; }

        public Int32 PoolId { get; set; }

        public String PoolName { get; set; }

        public String RootFolder { get; set; }

        public Boolean RunAsService { get; set;  }

        public string ServerUrl { get; set; }

        public String WorkFolder { get; set; }
    }
}