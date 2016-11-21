using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(MachineGroupServer))]
    public interface IMachineGroupServer : IAgentService
    {
        Task ConnectAsync(VssConnection agentConnection);

        // Configuration
        Task<List<DeploymentMachineGroup>> GetDeploymentMachineGroupsAsync(string projectName, string machineGroupName);

        // Update Machine Group ( Used for adding tags)
        Task<List<DeploymentMachine>> UpdateDeploymentMachineGroupAsync(string projectName, int machineGroupId, List<DeploymentMachine> deploymentMachines);
    }

    public sealed class MachineGroupServer : AgentService, IMachineGroupServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskAgentHttpClient _taskAgentClient;

        public async Task ConnectAsync(VssConnection agentConnection)
        {
            _connection = agentConnection;
            if (!_connection.HasAuthenticated)
            {
                await _connection.ConnectAsync();
            }

            _taskAgentClient = _connection.GetClient<TaskAgentHttpClient>();
            _hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        //-----------------------------------------------------------------
        // Configuration
        //-----------------------------------------------------------------
        public Task<List<DeploymentMachineGroup>> GetDeploymentMachineGroupsAsync(string projectName, string machineGroupName)
        {
            CheckConnection();
            return _taskAgentClient.GetDeploymentMachineGroupsAsync(projectName, machineGroupName);
        }

        //-----------------------------------------------------------------
        // Update
        //-----------------------------------------------------------------
        public Task<List<DeploymentMachine>> UpdateDeploymentMachineGroupAsync(string projectName, int machineGroupId, List<DeploymentMachine> deploymentMachines)
        {
            CheckConnection();
            return _taskAgentClient.UpdateDeploymentMachinesAsync(projectName, machineGroupId, deploymentMachines);
        }
    }
}