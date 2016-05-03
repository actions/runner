using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    [ServiceLocator(Default = typeof(CodeCoveragePublisher))]
    public interface ICodeCoveragePublisher : IAgentService
    {
        void InitializePublisher(IExecutionContext context, int buildId, string project, string projectCollectionUri, VssConnection connection);

        /// <summary>
        /// publish codecoverage summary data to server
        /// </summary>
        void PublishCodeCoverageSummary(IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken);

        /// <summary>
        /// publish codecoverage files to server
        /// </summary>
        void PublishCodeCoverageFiles(IEnumerable<Tuple<string, string>> files, CancellationToken cancellationToken, bool browsable);
    }

    internal class CodeCoveragePublisher : AgentService, ICodeCoveragePublisher
    {
        private ICodeCoverageServer _codeCoverageServer;
        private string _projectCollectionUri;
        private string _project;
        private int _buildId;

        public void InitializePublisher(IExecutionContext context, int buildId, string project, string projectCollectionUri, VssConnection connection)
        {
            _projectCollectionUri = projectCollectionUri;
            _buildId = buildId;
            _project = project;
            _codeCoverageServer = HostContext.GetService<ICodeCoverageServer>();
            _codeCoverageServer.InitializeServer(context, connection);
        }

        public void PublishCodeCoverageSummary(IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken)
        {
            _codeCoverageServer.PublishCoverageSummary(_project, _buildId, coverageData, cancellationToken);
        }

        public void PublishCodeCoverageFiles(IEnumerable<Tuple<string, string>> files, CancellationToken cancellationToken, bool browsable)
        {
            //Call Rest Api for uploading each of these files
            foreach (var file in files)
            {
                _codeCoverageServer.CreateArtifact(_project, _buildId, WellKnownArtifactResourceTypes.Container, file.Item2, file.Item1, browsable, cancellationToken);
            }
        }
    }
}
