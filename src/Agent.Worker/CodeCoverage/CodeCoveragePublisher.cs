using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.WebApi;
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
        Task PublishCodeCoverageSummaryAsync(IAsyncCommandContext context, IEnumerable<CodeCoverageStatistics> coverageData, string project, CancellationToken cancellationToken);

        /// <summary>
        /// publish codecoverage files to server
        /// </summary>
        Task PublishCodeCoverageFilesAsync(IAsyncCommandContext context, Guid projectId, long containerId, List<Tuple<string, string>> files, bool browsable, CancellationToken cancellationToken);
    }

    public sealed class CodeCoveragePublisher : AgentService, ICodeCoveragePublisher
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

        public async Task PublishCodeCoverageSummaryAsync(IAsyncCommandContext context, IEnumerable<CodeCoverageStatistics> coverageData, string project, CancellationToken cancellationToken)
        {
            await _codeCoverageServer.PublishCoverageSummaryAsync(context, _connection, project, _buildId, coverageData, cancellationToken);
        }

        public async Task PublishCodeCoverageFilesAsync(IAsyncCommandContext context, Guid projectId, long containerId, List<Tuple<string, string>> files, bool browsable, CancellationToken cancellationToken)
        {
            var publishCCTasks = files.Select(async file =>
            {
                var browsableProperty = (browsable) ? bool.TrueString : bool.FalseString;
                var artifactProperties = new Dictionary<string, string> {
                    { ArtifactUploadEventProperties.ContainerFolder, file.Item2},
                    { ArtifactUploadEventProperties.ArtifactName, file.Item2 },
                    { ArtifactAssociateEventProperties.ArtifactType, ArtifactResourceTypes.Container },
                    { ArtifactAssociateEventProperties.Browsable, browsableProperty },
                };

                FileContainerServer fileContainerHelper = new FileContainerServer(_connection, projectId, containerId, file.Item2);
                await fileContainerHelper.CopyToContainerAsync(context, file.Item1, cancellationToken);
                string fileContainerFullPath = StringUtil.Format($"#/{containerId}/{file.Item2}");

                Build.BuildServer buildHelper = new Build.BuildServer(_connection, projectId);
                var artifact = await buildHelper.AssociateArtifact(_buildId, file.Item2, ArtifactResourceTypes.Container, fileContainerFullPath, artifactProperties, cancellationToken);
                context.Output(StringUtil.Loc("PublishedCodeCoverageArtifact", file.Item1, file.Item2));
            });

            await Task.WhenAll(publishCCTasks);
        }
    }
}
