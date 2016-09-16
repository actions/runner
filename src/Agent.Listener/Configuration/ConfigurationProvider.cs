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

        Task<TaskAgent> UpdateAgentAsync(int poolId, TaskAgent agent);

        Task<TaskAgent> AddAgentAsync(int poolId, TaskAgent agent);

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
            return command.GetUrl(StringUtil.Loc("ServerUrl"));
        }

        public async Task<int> GetPoolId(CommandSettings command)
        {
            int poolId = 0;
            string poolName;
            while (true)
            {
                poolName = command.GetPool();
                try
                {
                    poolId = await GetPoolIdAsync(poolName);
                    Trace.Info($"PoolId for agent pool '{poolName}' is '{poolId}'.");
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                    _term.WriteError(StringUtil.Loc("FailedToFindPool"));
                }
            }

            return poolId;
        }

        public Task<TaskAgent> UpdateAgentAsync(int poolId, TaskAgent agent)
        {
            return _agentServer.UpdateAgentAsync(poolId, agent);
        }

        public Task<TaskAgent> AddAgentAsync(int poolId, TaskAgent agent)
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

        private string _projectName;
        private string _collectionName;
        private string _machineGroupName;
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
            _serverUrl =  command.GetUrl(StringUtil.Loc("ServerUrlForMachineGroupAgent"));
            Trace.Info("url - {0}", _serverUrl);

            string baseUrl = _serverUrl;
            _isHosted = UrlUtil.IsHosted(_serverUrl);

            // VSTS account url - Do validation of server Url includes project name 
            // On-prem tfs Url - Do validation of tfs Url includes collection and project name 

            Uri uri = new Uri(_serverUrl);                                   //e.g On-prem => http://myonpremtfs:8080/tfs/defaultcollection/myproject
                                                                             //e.g VSTS => https://myvstsaccount.visualstudio.com/myproject

            string urlAbsolutePath = uri.AbsolutePath;                       //e.g tfs/defaultcollection/myproject
                                                                             //e.g myproject
            string[] urlTokenParts = urlAbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);      //e.g tfs,defaultcollection,myproject
            int tokenCount = urlTokenParts.Length;

            if (tokenCount == 0)
            {
                if (! _isHosted)
                {
                    throw new Exception(StringUtil.Loc("UrlValidationFailedForOnPremTfs"));
                }
                else
                {
                    throw new Exception(StringUtil.Loc("UrlValidationFailedForVSTSAccount"));
                }
            }
            
            // for onprem ensure collection/project is format
            if (! _isHosted)
            {
                Trace.Info("Provided url is for onprem tfs");
                
                if (tokenCount <= 1)
                {
                    throw new Exception(StringUtil.Loc("UrlValidationFailedForOnPremTfs"));
                }
                _collectionName = urlTokenParts[tokenCount-2];
                _projectName = urlTokenParts[tokenCount-1];
                Trace.Info("collectionName - {0}", _collectionName);

                baseUrl = _serverUrl.Replace(_projectName, "").Replace(_collectionName, "").TrimEnd(new char[] { '/'});
            }
            else
            {
                Trace.Info("Provided url is for vsts account");
                _projectName = urlTokenParts.Last();

                baseUrl = new Uri(_serverUrl).GetLeftPart(UriPartial.Authority);
            }

            Trace.Info("projectName - {0}", _projectName);

            return baseUrl;
        }

        public async Task<int> GetPoolId(CommandSettings command)
        {
            int poolId;
            while (true)
            {
                _machineGroupName = command.GetMachineGroupName();
                try
                {
                    poolId =  await GetPoolIdAsync(_projectName, _machineGroupName);
                    Trace.Info($"PoolId for machine group '{_machineGroupName}' is '{poolId}'.");
                    break;
                }
                catch (Exception e) when (!command.Unattended)
                {
                    _term.WriteError(e);
                }

                _term.WriteError(StringUtil.Loc("FailedToFindMachineGroup"));

                // In case of failure ensure to get the project name again
                _projectName = command.GetProjectName(_projectName);
            }
            
            return poolId;
        }

        public Task<TaskAgent> UpdateAgentAsync(int poolId, TaskAgent agent)
        {
            return _agentServer.UpdateAgentAsync(poolId, agent);
        }

        public Task<TaskAgent> AddAgentAsync(int poolId, TaskAgent agent)
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
            settings.MachineGroupName = _machineGroupName;
            settings.ProjectName = _projectName;
        }

        private async Task<int> GetPoolIdAsync(string projectName, string machineGroupName)
        {
            ArgUtil.NotNull(_machineGroupServer, nameof(_machineGroupServer));

            DeploymentMachineGroup machineGroup = (await _machineGroupServer.GetDeploymentMachineGroupsAsync(projectName, machineGroupName)).FirstOrDefault();

            if (machineGroup == null)
            {
                throw new DeploymentMachineGroupNotFoundException(StringUtil.Loc("MachineGroupNotFound", machineGroupName));
            }

            Trace.Info("Found machine group {0} with id {1}", machineGroupName, machineGroup.Id);
            Trace.Info("Found poolId {0} for machine group {1}", machineGroup.Pool.Id, machineGroupName);

            return machineGroup.Pool.Id;
        }
    }
}
