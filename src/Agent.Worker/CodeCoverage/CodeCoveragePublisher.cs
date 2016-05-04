using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    [ServiceLocator(Default = typeof(CodeCoveragePublisher))]
    public interface ICodeCoveragePublisher : IAgentService
    {
        void InitializePublisher(int buildId, VssConnection connection);

        /// <summary>
        /// publish codecoverage summary data to server
        /// </summary>
        Task PublishCodeCoverageSummaryAsync(IEnumerable<CodeCoverageStatistics> coverageData, string project, CancellationToken cancellationToken);

        /// <summary>
        /// publish codecoverage files to server
        /// </summary>
        Task PublishCodeCoverageFilesAsync(IAsyncCommandContext context, Guid projectId, long containerId, IEnumerable<Tuple<string, string>> files, bool browsable, CancellationToken cancellationToken);
    }

    internal class CodeCoveragePublisher : AgentService, ICodeCoveragePublisher
    {
        private ICodeCoverageServer _codeCoverageServer;
        private int _buildId;
        private VssConnection _connection;

        public void InitializePublisher(int buildId, VssConnection connection)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            _connection = connection;
            _buildId = buildId;
            _codeCoverageServer = HostContext.GetService<ICodeCoverageServer>();
        }

        public async Task PublishCodeCoverageSummaryAsync(IEnumerable<CodeCoverageStatistics> coverageData, string project, CancellationToken cancellationToken)
        {
            await _codeCoverageServer.PublishCoverageSummaryAsync(_connection, project, _buildId, coverageData, cancellationToken);
        }

        public async Task PublishCodeCoverageFilesAsync(IAsyncCommandContext context, Guid projectId, long containerId, IEnumerable<Tuple<string, string>> files, bool browsable, CancellationToken cancellationToken)
        {
            var publishCCTasks = files.Select(file =>
            {
                return _codeCoverageServer.CreateArtifactAsync(context, _connection, projectId, _buildId, containerId, file.Item2, file.Item1, browsable, cancellationToken);
            });
            await Task.WhenAll(publishCCTasks);
        }
    }
}
