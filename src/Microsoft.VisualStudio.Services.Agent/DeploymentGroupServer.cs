using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(DeploymentGroupServer))]
    public interface IDeploymentGroupServer : IAgentService
    {
        Task ConnectAsync(VssConnection agentConnection);

        // Configuration
        Task<List<DeploymentGroup>> GetDeploymentGroupsAsync(string projectName, string machineGroupName);

        // Update Machine Group ( Used for adding tags)
        Task<List<DeploymentMachine>> UpdateDeploymentMachinesAsync(Guid projectId, int deploymentGroupId, List<DeploymentMachine> deploymentMachine);

        // Add Deployment Machine
        Task<DeploymentMachine> AddDeploymentMachineAsync(Guid projectId, int deploymentGroupId, DeploymentMachine machine);

        // Replace Deployment Machine
        Task<DeploymentMachine> ReplaceDeploymentMachineAsync(Guid projectId, int deploymentGroupId, int machineId, DeploymentMachine machine);

        // Delete Deployment Machine
        Task DeleteDeploymentMachineAsync(string projectName, int deploymentGroupId, int machineId);

        Task DeleteDeploymentMachineAsync(Guid projectId, int deploymentGroupId, int machineId);

        // Get Deployment Machines
        Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(string projectName, int deploymentGroupId, string machineName);

        Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(Guid projectGuid, int deploymentGroupId, string machineName);
    }

    public sealed class DeploymentGroupServer : AgentService, IDeploymentGroupServer
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
        public Task<List<DeploymentGroup>> GetDeploymentGroupsAsync(string projectName, string machineGroupName)
        {
            CheckConnection();
            return _taskAgentClient.GetDeploymentGroupsAsync(projectName, machineGroupName);
        }

        public Task<DeploymentMachine> AddDeploymentMachineAsync(Guid projectId, int deploymentGroupId, DeploymentMachine machine)
        {
            CheckConnection();
            return _taskAgentClient.AddDeploymentMachineAsync(projectId, deploymentGroupId, machine);
        }

        public Task<DeploymentMachine> ReplaceDeploymentMachineAsync(Guid projectId, int deploymentGroupId, int machineId, DeploymentMachine machine)
        {
            CheckConnection();
            return _taskAgentClient.ReplaceDeploymentMachineAsync(projectId, deploymentGroupId, machineId, machine);
        }

        public Task DeleteDeploymentMachineAsync(string projectName, int deploymentGroupId, int machineId)
        {
            CheckConnection();
            return _taskAgentClient.DeleteDeploymentMachineAsync(projectName, deploymentGroupId, machineId);
        }

        public Task DeleteDeploymentMachineAsync(Guid projectId, int deploymentGroupId, int machineId)
        {
            CheckConnection();
            return _taskAgentClient.DeleteDeploymentMachineAsync(projectId, deploymentGroupId, machineId);
        }

        public Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(string projectName, int deploymentGroupId, string machineName)
        {
            CheckConnection();
            return _taskAgentClient.GetDeploymentMachinesAsync(projectName, deploymentGroupId, null, machineName);
        }

        public Task<List<DeploymentMachine>> GetDeploymentMachinesAsync(Guid projectGuid, int deploymentGroupId, string machineName)
        {
            CheckConnection();
            return _taskAgentClient.GetDeploymentMachinesAsync(projectGuid, deploymentGroupId, null, machineName);
        }

        //-----------------------------------------------------------------
        // Update
        //-----------------------------------------------------------------
        public Task<List<DeploymentMachine>> UpdateDeploymentMachinesAsync(Guid projectId, int deploymentGroupId, List<DeploymentMachine> deploymentMachine)
        {
            CheckConnection();
            return _taskAgentClient.UpdateDeploymentMachinesAsync(projectId, deploymentGroupId, deploymentMachine);
        }
    }
}