using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(JobServer))]
    public interface IJobServer : IAgentService
    {
        Task ConnectAsync(VssConnection jobConnection);

        // logging and console
        Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken);
        Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, IEnumerable<string> lines, CancellationToken cancellationToken);
        Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, String type, String name, Stream uploadStream, CancellationToken cancellationToken);
        Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, string path, CancellationToken cancellationToken);
        Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken);
        Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken);

        // task download
        Task<Stream> GetTaskContentZipAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token);
        Task<TaskDefinition> GetTaskDefinitionAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token);
    }

    public sealed class JobServer : AgentService, IJobServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskHttpClient _taskClient;
        private TaskAgentHttpClient _taskAgentClient;

        public async Task ConnectAsync(VssConnection jobConnection)
        {
            _connection = jobConnection;

            if (!_connection.HasAuthenticated)
            {
                await _connection.ConnectAsync();
            }

            _taskClient = _connection.GetClient<TaskHttpClient>();
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
        // Feedback: WebConsole, TimelineRecords and Logs
        //-----------------------------------------------------------------

        public Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, IEnumerable<string> lines, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, string type, string name, Stream uploadStream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        //-----------------------------------------------------------------
        // Task Manager: Query and Download Task
        //-----------------------------------------------------------------

        public Task<TaskDefinition> GetTaskDefinitionAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token)
        {
            return _taskAgentClient.GetTaskDefinitionAsync(taskId, taskVersion, null, null, null, token);
        }

        public Task<Stream> GetTaskContentZipAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token)
        {
            return _taskAgentClient.GetTaskContentZipAsync(taskId, taskVersion, null, token);
        }
    }
}