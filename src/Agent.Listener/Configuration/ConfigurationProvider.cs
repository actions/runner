using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public interface IConfigurationProvider : IExtension, IAgentService
    {
        string ConfigurationProviderType { get; }

        void GetServerUrl(AgentSettings agentSettings, CommandSettings command);

        void GetCollectionName(AgentSettings agentSettings, CommandSettings command, bool isHosted);

        Task TestConnectionAsync(AgentSettings agentSettings, VssCredentials creds, bool isHosted);

        Task GetPoolIdAndName(AgentSettings agentSettings, CommandSettings command);

        string GetFailedToFindPoolErrorString();

        Task<TaskAgent> UpdateAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command);

        Task<TaskAgent> AddAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command);

        Task DeleteAgentAsync(AgentSettings agentSettings);

        Task<TaskAgent> GetAgentAsync(AgentSettings agentSettings);

        void ThrowTaskAgentExistException(AgentSettings agentSettings);
    }

    public class BuildReleasesAgentConfigProvider : AgentService, IConfigurationProvider
    {
        public Type ExtensionType => typeof(IConfigurationProvider);
        private ITerminal _term;
        protected IAgentServer _agentServer;

        public string ConfigurationProviderType
            => Constants.Agent.AgentConfigurationProvider.BuildReleasesAgentConfiguration;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
            _agentServer = HostContext.GetService<IAgentServer>();
        }

        public void GetServerUrl(AgentSettings agentSettings, CommandSettings command)
        {
            agentSettings.ServerUrl = command.GetUrl();
        }

        public void GetCollectionName(AgentSettings agentSettings, CommandSettings command, bool isHosted)
        {
            // Collection name is not required for Build/Release agent
        }

        public virtual async Task GetPoolIdAndName(AgentSettings agentSettings, CommandSettings command)
        {
            string poolName = command.GetPool();

            TaskAgentPool agentPool = (await _agentServer.GetAgentPoolsAsync(poolName)).FirstOrDefault();
            if (agentPool == null)
            {
                throw new TaskAgentPoolNotFoundException(StringUtil.Loc("PoolNotFound", poolName));
            }
            else
            {
                Trace.Info("Found pool {0} with id {1} and name {2}", poolName, agentPool.Id, agentPool.Name);
                agentSettings.PoolId = agentPool.Id;
                agentSettings.PoolName = agentPool.Name;
            }
        }

        public string GetFailedToFindPoolErrorString() => StringUtil.Loc("FailedToFindPool");

        public void ThrowTaskAgentExistException(AgentSettings agentSettings)
        {
            throw new TaskAgentExistsException(StringUtil.Loc("AgentWithSameNameAlreadyExistInPool", agentSettings.PoolId, agentSettings.AgentName));
        }

        public Task<TaskAgent> UpdateAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command)
        {
            return _agentServer.UpdateAgentAsync(agentSettings.PoolId, agent);
        }

        public Task<TaskAgent> AddAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command)
        {
            return _agentServer.AddAgentAsync(agentSettings.PoolId, agent);
        }

        public Task DeleteAgentAsync(AgentSettings agentSettings)
        {
            return _agentServer.DeleteAgentAsync(agentSettings.PoolId, agentSettings.AgentId);
        }

        public async Task TestConnectionAsync(AgentSettings agentSettings, VssCredentials creds, bool isHosted)
        {
            _term.WriteLine(StringUtil.Loc("ConnectingToServer"));
            await _agentServer.ConnectAsync(new Uri(agentSettings.ServerUrl), creds);
        }

        public async Task<TaskAgent> GetAgentAsync(AgentSettings agentSettings)
        {
            var agents = await _agentServer.GetAgentsAsync(agentSettings.PoolId, agentSettings.AgentName);
            Trace.Verbose("Returns {0} agents", agents.Count);
            return agents.FirstOrDefault();
        }
    }
}
