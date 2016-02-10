using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
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
        Boolean RunAsWindowsService { get; }
        String ServerUrl { get; }
        String WorkFolder { get; }
        Task SaveAsync();
    }

    public sealed class AgentSettings : IAgentSettings
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

        public async Task SaveAsync()
        {
            await Task.Yield();
        }
    }
}