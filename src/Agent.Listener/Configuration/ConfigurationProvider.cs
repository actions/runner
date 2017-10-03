using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Common;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public interface IConfigurationProvider : IExtension, IAgentService
    {
        string ConfigurationProviderType { get; }

        void GetServerUrl(AgentSettings agentSettings, CommandSettings command);

        Task TestConnectionAsync(AgentSettings agentSettings, VssCredentials creds);

        Task GetPoolId(AgentSettings agentSettings, CommandSettings command);

        string GetFailedToFindPoolErrorString();

        Task<TaskAgent> UpdateAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command);

        Task<TaskAgent> AddAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command);

        Task DeleteAgentAsync(AgentSettings agentSettings);

        Task<TaskAgent> GetAgentAsync(AgentSettings agentSettings);

        void ThrowTaskAgentExistException(AgentSettings agentSettings);
    }

    public sealed class BuildReleasesAgentConfigProvider : AgentService, IConfigurationProvider
    {
        public Type ExtensionType => typeof(IConfigurationProvider);
        private ITerminal _term;
        private IAgentServer _agentServer;

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

        public async Task GetPoolId(AgentSettings agentSettings, CommandSettings command)
        {
            int poolId = 0;
            string poolName;

            poolName = command.GetPool();
            poolId = await GetPoolIdAsync(poolName);
            Trace.Info($"PoolId for agent pool '{poolName}' is '{poolId}'.");

            agentSettings.PoolId = poolId;
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

        public async Task TestConnectionAsync(AgentSettings agentSettings, VssCredentials creds)
        {
            _term.WriteLine(StringUtil.Loc("ConnectingToServer"));
            VssConnection connection = ApiUtil.CreateConnection(new Uri(agentSettings.ServerUrl), creds);

            await _agentServer.ConnectAsync(connection);
        }

        public async Task<TaskAgent> GetAgentAsync(AgentSettings agentSettings)
        {
            var agents = await _agentServer.GetAgentsAsync(agentSettings.PoolId, agentSettings.AgentName);
            Trace.Verbose("Returns {0} agents", agents.Count);
            return agents.FirstOrDefault();
        }

        private async Task<int> GetPoolIdAsync(string poolName)
        {
            TaskAgentPool agentPool = (await _agentServer.GetAgentPoolsAsync(poolName)).FirstOrDefault();
            if (agentPool == null)
            {
                throw new TaskAgentPoolNotFoundException(StringUtil.Loc("PoolNotFound", poolName));
            }
            else
            {
                Trace.Info("Found pool {0} with id {1}", poolName, agentPool.Id);
                return agentPool.Id;
            }
        }
    }

    public sealed class DeploymentGroupAgentConfigProvider : AgentService, IConfigurationProvider
    {
        public Type ExtensionType => typeof(IConfigurationProvider);
        private ITerminal _term;
        private string _projectName = string.Empty;
        private IDeploymentGroupServer _deploymentGroupServer = null;

        public string ConfigurationProviderType
            => Constants.Agent.AgentConfigurationProvider.DeploymentAgentConfiguration;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
            _deploymentGroupServer = HostContext.GetService<IDeploymentGroupServer>();
        }

        public void GetServerUrl(AgentSettings agentSettings, CommandSettings command)
        {
            agentSettings.ServerUrl = command.GetUrl();
            Trace.Info("url - {0}", agentSettings.ServerUrl);

            var isHosted = UrlUtil.IsHosted(agentSettings.ServerUrl);

            // for onprem tfs, collection is required for deploymentGroup
            if (!isHosted)
            {
                Trace.Info("Provided url is for onprem tfs, need collection name");
                agentSettings.CollectionName = command.GetCollectionName();
            }
        }

        public async Task GetPoolId(AgentSettings agentSettings, CommandSettings command)
        {
            _projectName = command.GetProjectName(_projectName);
            var deploymentGroupName = command.GetDeploymentGroupName();

            var deploymentGroup = await GetDeploymentGroupAsync(_projectName, deploymentGroupName);
            Trace.Info($"PoolId for deployment group '{deploymentGroupName}' is '{deploymentGroup.Pool.Id}'.");
            Trace.Info($"Project id for deployment group '{deploymentGroupName}' is '{deploymentGroup.Project.Id.ToString()}'.");

            agentSettings.PoolId = deploymentGroup.Pool.Id;
            agentSettings.DeploymentGroupId = deploymentGroup.Id;
            agentSettings.ProjectId = deploymentGroup.Project.Id.ToString();
        }

        public string GetFailedToFindPoolErrorString() => StringUtil.Loc("FailedToFindDeploymentGroup");

        public void ThrowTaskAgentExistException(AgentSettings agentSettings)
        {
            throw new TaskAgentExistsException(StringUtil.Loc("DeploymentMachineWithSameNameAlreadyExistInDeploymentGroup", agentSettings.DeploymentGroupId, agentSettings.AgentName));
        }

        public async Task<TaskAgent> UpdateAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command)
        {
            var deploymentMachine = (await this.GetDeploymentMachinesAsync(agentSettings)).FirstOrDefault();

            deploymentMachine.Agent = agent;
            deploymentMachine = await _deploymentGroupServer.ReplaceDeploymentMachineAsync(new Guid(agentSettings.ProjectId), agentSettings.DeploymentGroupId, deploymentMachine.Id, deploymentMachine);

            await GetAndAddTags(deploymentMachine, agentSettings, command);
            return deploymentMachine.Agent;
        }

        public async Task<TaskAgent> AddAgentAsync(AgentSettings agentSettings, TaskAgent agent, CommandSettings command)
        {
            var deploymentMachine = new DeploymentMachine() { Agent = agent };
            deploymentMachine = await _deploymentGroupServer.AddDeploymentMachineAsync(new Guid(agentSettings.ProjectId), agentSettings.DeploymentGroupId, deploymentMachine);

            await GetAndAddTags(deploymentMachine, agentSettings, command);

            return deploymentMachine.Agent;
        }

        public async Task DeleteAgentAsync(AgentSettings agentSettings)
        {
            var machines = await GetDeploymentMachinesAsync(agentSettings);
            Trace.Verbose("Returns {0} machines with name {1}", machines.Count, agentSettings.AgentName);
            var machine = machines.FirstOrDefault();
            if (machine != null)
            {
                if (!string.IsNullOrWhiteSpace(agentSettings.ProjectId))
                {
                    await _deploymentGroupServer.DeleteDeploymentMachineAsync(new Guid(agentSettings.ProjectId), agentSettings.DeploymentGroupId, machine.Id);
                }
                else
                {
                    await _deploymentGroupServer.DeleteDeploymentMachineAsync(agentSettings.ProjectName, agentSettings.DeploymentGroupId, machine.Id);
                }
            }
        }

        public async Task TestConnectionAsync(AgentSettings agentSettings, VssCredentials creds)
        {
            var url = agentSettings.ServerUrl;  // Ensure not to update back the url with agentSettings !!!
            var isHosted = UrlUtil.IsHosted(url);
            _term.WriteLine(StringUtil.Loc("ConnectingToServer"));

            // Create the connection for deployment group 
            Trace.Info("Test connection with deployment group");
            if (!isHosted && !string.IsNullOrWhiteSpace(agentSettings.CollectionName)) // For on-prm validate the collection by making the connection
            {
                UriBuilder uriBuilder = new UriBuilder(new Uri(url));
                uriBuilder.Path = uriBuilder.Path + "/" + agentSettings.CollectionName;
                Trace.Info("Tfs Collection level url to connect - {0}", uriBuilder.Uri.AbsoluteUri);
                url = uriBuilder.Uri.AbsoluteUri;
            }
            VssConnection deploymentGroupconnection = ApiUtil.CreateConnection(new Uri(url), creds);

            await _deploymentGroupServer.ConnectAsync(deploymentGroupconnection);
            Trace.Info("Connect complete for deployment group");
        }

        public async Task<TaskAgent> GetAgentAsync(AgentSettings agentSettings)
        {
            var machines = await GetDeploymentMachinesAsync(agentSettings);
            Trace.Verbose("Returns {0} machines", machines.Count);
            var machine = machines.FirstOrDefault();
            if (machine != null)
            {
                return machine.Agent;
            }

            return null;
        }

        private async Task GetAndAddTags(DeploymentMachine deploymentMachine, AgentSettings agentSettings, CommandSettings command)
        {
            // Get and apply Tags in case agent is configured against Deployment Group
            bool needToAddTags = command.GetDeploymentGroupTagsRequired();
            while (needToAddTags)
            {
                try
                {
                    string tagString = command.GetDeploymentGroupTags();
                    Trace.Info("Given tags - {0} will be processed and added", tagString);

                    if (!string.IsNullOrWhiteSpace(tagString))
                    {
                        var tagsList =
                            tagString.Split(',').Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s.Trim())
                                .Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                        if (tagsList.Any())
                        {
                            Trace.Info("Adding tags - {0}", string.Join(",", tagsList.ToArray()));

                            deploymentMachine.Tags = tagsList;
                            await _deploymentGroupServer.UpdateDeploymentMachinesAsync(new Guid(agentSettings.ProjectId), agentSettings.DeploymentGroupId, new List<DeploymentMachine>() { deploymentMachine });

                            _term.WriteLine(StringUtil.Loc("DeploymentGroupTagsAddedMsg"));
                        }
                    }
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                    _term.WriteError(StringUtil.Loc("FailedToAddTags"));
                }
            }
        }

        private async Task<DeploymentGroup> GetDeploymentGroupAsync(string projectName, string deploymentGroupName)
        {
            ArgUtil.NotNull(_deploymentGroupServer, nameof(_deploymentGroupServer));

            var deploymentGroup = (await _deploymentGroupServer.GetDeploymentGroupsAsync(projectName, deploymentGroupName)).FirstOrDefault();

            if (deploymentGroup == null)
            {
                throw new DeploymentGroupNotFoundException(StringUtil.Loc("DeploymentGroupNotFound", deploymentGroupName));
            }

            Trace.Info("Found deployment group {0} with id {1}", deploymentGroupName, deploymentGroup.Id);
            return deploymentGroup;
        }

        private async Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(AgentSettings agentSettings)
        {
            List<DeploymentMachine> machines;
            if (!string.IsNullOrWhiteSpace(agentSettings.ProjectId))
            {
                machines = await _deploymentGroupServer.GetDeploymentMachinesAsync(new Guid(agentSettings.ProjectId), agentSettings.DeploymentGroupId, agentSettings.AgentName);
            }
            else
            {
                machines = await _deploymentGroupServer.GetDeploymentMachinesAsync(agentSettings.ProjectName, agentSettings.DeploymentGroupId, agentSettings.AgentName);
            }

            return machines;
        }
    }
}
