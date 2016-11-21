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

        string GetServerUrl(CommandSettings command);

        Task TestConnectionAsync(string tfsUrl, VssCredentials creds);

        Task<int> GetPoolId(CommandSettings command);

        string GetFailedToFindPoolErrorString();

        Task<TaskAgent> UpdateAgentAsync(int poolId, TaskAgent agent, CommandSettings command);

        Task<TaskAgent> AddAgentAsync(int poolId, TaskAgent agent, CommandSettings command);

        Task DeleteAgentAsync(int agentPoolId, int agentId);

        void UpdateAgentSetting(AgentSettings settings);
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

        public void UpdateAgentSetting(AgentSettings settings)
        {
            // No implementation required
        }

        public string GetServerUrl(CommandSettings command)
        {
            return command.GetUrl();
        }

        public async Task<int> GetPoolId(CommandSettings command)
        {
            int poolId = 0;
            string poolName;

            poolName = command.GetPool();
            poolId = await GetPoolIdAsync(poolName);
            Trace.Info($"PoolId for agent pool '{poolName}' is '{poolId}'.");
 
            return poolId;
        }

        public string GetFailedToFindPoolErrorString() => StringUtil.Loc("FailedToFindPool");

        public Task<TaskAgent> UpdateAgentAsync(int poolId, TaskAgent agent, CommandSettings command)
        {
            return _agentServer.UpdateAgentAsync(poolId, agent);
        }

        public Task<TaskAgent> AddAgentAsync(int poolId, TaskAgent agent, CommandSettings command)
        { 
            return _agentServer.AddAgentAsync(poolId, agent);
        }

        public Task DeleteAgentAsync(int agentPoolId, int agentId)
        {
            return _agentServer.DeleteAgentAsync(agentPoolId, agentId);
        }

        public async Task TestConnectionAsync(string url, VssCredentials creds)
        {
            _term.WriteLine(StringUtil.Loc("ConnectingToServer"));
            VssConnection connection = ApiUtil.CreateConnection(new Uri(url), creds);

            await _agentServer.ConnectAsync(connection);
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

    public sealed class MachineGroupAgentConfigProvider : AgentService, IConfigurationProvider
    {
        public Type ExtensionType => typeof(IConfigurationProvider);
        private ITerminal _term;
        private IAgentServer _agentServer;

        private string _projectName = string.Empty;
        private string _collectionName;
        private int _machineGroupId;
        private string _serverUrl;
        private bool _isHosted = false;
        private IMachineGroupServer _machineGroupServer = null;

        public string ConfigurationProviderType
            => Constants.Agent.AgentConfigurationProvider.DeploymentAgentConfiguration;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
            _agentServer = HostContext.GetService<IAgentServer>();
            _machineGroupServer = HostContext.GetService<IMachineGroupServer>();
        }

        public string GetServerUrl(CommandSettings command)
        {
            _serverUrl =  command.GetUrl();
            Trace.Info("url - {0}", _serverUrl);

            _isHosted = UrlUtil.IsHosted(_serverUrl);

            // for onprem tfs, collection is required for machineGroup
            if (! _isHosted)
            {
                Trace.Info("Provided url is for onprem tfs, need collection name");
                _collectionName = command.GetCollectionName();
            }

            return _serverUrl;
        }

        public async Task<int> GetPoolId(CommandSettings command)
        {
            int poolId;

            _projectName = command.GetProjectName(_projectName);
            var machineGroupName = command.GetMachineGroupName();

            poolId =  await GetPoolIdAsync(_projectName, machineGroupName);
            Trace.Info($"PoolId for machine group '{machineGroupName}' is '{poolId}'.");
            
            return poolId;
        }

        public string GetFailedToFindPoolErrorString() => StringUtil.Loc("FailedToFindMachineGroup");

        public async Task<TaskAgent> UpdateAgentAsync(int poolId, TaskAgent agent, CommandSettings command)
        {
            agent = await _agentServer.UpdateAgentAsync(poolId, agent);
            await GetAndAddTags(agent, command);

            return agent;
        }

        public async Task<TaskAgent> AddAgentAsync(int poolId, TaskAgent agent, CommandSettings command)
        {
            agent = await _agentServer.AddAgentAsync(poolId, agent);
            await GetAndAddTags(agent, command);

            return agent;
        }

        public Task DeleteAgentAsync(int agentPoolId, int agentId)
        {
            return _agentServer.DeleteAgentAsync(agentPoolId, agentId);
        }

        public async Task TestConnectionAsync(string url, VssCredentials creds)
        {
            _term.WriteLine(StringUtil.Loc("ConnectingToServer"));
            VssConnection connection = ApiUtil.CreateConnection(new Uri(url), creds);

            await _agentServer.ConnectAsync(connection);
            Trace.Info("Connect complete for server");

            // Create the connection for machine group 
            Trace.Info("Test connection with machine group");
            if (!_isHosted && !_collectionName.IsNullOrEmpty()) // For on-prm validate the collection by making the connection
            {
                UriBuilder uriBuilder = new UriBuilder(new Uri(url));
                uriBuilder.Path = uriBuilder.Path + "/" + _collectionName;
                Trace.Info("Tfs Collection level url to connect - {0}", uriBuilder.Uri.AbsoluteUri);
                url = uriBuilder.Uri.AbsoluteUri;
            }
            VssConnection machineGroupconnection = ApiUtil.CreateConnection(new Uri(url), creds);

            await _machineGroupServer.ConnectAsync(machineGroupconnection);
            Trace.Info("Connect complete for machine group");
        }

        public void UpdateAgentSetting(AgentSettings settings)
        {
            settings.MachineGroupId = _machineGroupId;
            settings.ProjectName = _projectName;
        }

        private async Task GetAndAddTags(TaskAgent agent, CommandSettings command)
        {
            // Get and apply Tags in case agent is configured against Machine Group
            bool needToAddTags = command.GetMachineGroupTagsRequired();
            while (needToAddTags)
            {
                try
                {
                    string tagString = command.GetMachineGroupTags();
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

                            var deploymentMachine = new DeploymentMachine()
                            {
                                Agent = agent,
                                Tags = tagsList
                            };

                            await _machineGroupServer.UpdateDeploymentMachineGroupAsync(_projectName, _machineGroupId,
                                           new List<DeploymentMachine>() { deploymentMachine });

                            _term.WriteLine(StringUtil.Loc("MachineGroupTagsAddedMsg"));
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

        private async Task<int> GetPoolIdAsync(string projectName, string machineGroupName)
        {
            ArgUtil.NotNull(_machineGroupServer, nameof(_machineGroupServer));

            DeploymentMachineGroup machineGroup = (await _machineGroupServer.GetDeploymentMachineGroupsAsync(projectName, machineGroupName)).FirstOrDefault();

            if (machineGroup == null)
            {
                throw new DeploymentMachineGroupNotFoundException(StringUtil.Loc("MachineGroupNotFound", machineGroupName));
            }

            _machineGroupId = machineGroup.Id;
            Trace.Info("Found machine group {0} with id {1}", machineGroupName, machineGroup.Id);
            Trace.Info("Found poolId {0} for machine group {1}", machineGroup.Pool.Id, machineGroupName);

            return machineGroup.Pool.Id;
        }
    }
}
