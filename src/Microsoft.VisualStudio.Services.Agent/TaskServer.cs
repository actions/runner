using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(TaskServer))]
    public interface ITaskServer : IAgentService
    {
        Task ConnectAsync(VssConnection jobConnection);

        // task download
        Task<Stream> GetTaskContentZipAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token);

        Task<bool> TaskDefinitionEndpointExist(CancellationToken token);
    }

    public sealed class TaskServer : AgentService, ITaskServer
    {
        private bool _hasConnection;

        private VssConnection _connection;

        private TaskAgentHttpClient _taskAgentClient;

        public async Task ConnectAsync(VssConnection jobConnection)
        {
            _connection = jobConnection;

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
        // Task Manager: Query and Download Task
        //-----------------------------------------------------------------
        public Task<Stream> GetTaskContentZipAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token)
        {
            CheckConnection();
            return _taskAgentClient.GetTaskContentZipAsync(taskId, taskVersion, null, token);
        }

        public async Task<bool> TaskDefinitionEndpointExist(CancellationToken token)
        {
            CheckConnection();
            try
            {
                var definitions = await _taskAgentClient.GetTaskDefinitionsAsync(cancellationToken: token);
            }
            catch (VssResourceNotFoundException)
            {
                return false;
            }

            return true;
        }
    }
}