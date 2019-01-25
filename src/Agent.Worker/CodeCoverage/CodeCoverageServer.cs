using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.FeatureAvailability.WebApi;
using Microsoft.VisualStudio.Services.TestResults.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    [ServiceLocator(Default = typeof(CodeCoverageServer))]
    public interface ICodeCoverageServer : IAgentService
    {
        /// <summary>
        /// Publish code coverage summary
        /// </summary>
        Task PublishCoverageSummaryAsync(IAsyncCommandContext context, VssConnection connection, string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken);
    }

    internal sealed class CodeCoverageServer : AgentService, ICodeCoverageServer
    {
        public async Task PublishCoverageSummaryAsync(IAsyncCommandContext context, VssConnection connection, string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken)
        {
            // <todo: Bug 402783> We are currently passing BuildFlavor and BuildPlatform = "" There value are required be passed to command
            CodeCoverageData data = new CodeCoverageData()
            {
                BuildFlavor = "",
                BuildPlatform = "",
                CoverageStats = coverageData.ToList()
            };

            FeatureAvailabilityHttpClient featureAvailabilityHttpClient = connection.GetClient<FeatureAvailabilityHttpClient>();
            if (FeatureFlagUtility.GetFeatureFlagState(featureAvailabilityHttpClient, CodeCoverageConstants.EnablePublishToTcmServiceDirectlyFromTaskFF, context))
            {
                TestResultsHttpClient tcmClient = connection.GetClient<TestResultsHttpClient>();
                await tcmClient.UpdateCodeCoverageSummaryAsync(data, project, buildId, cancellationToken: cancellationToken);
            }
            else
            {
                TestManagementHttpClient tfsClient = connection.GetClient<TestManagementHttpClient>();
                await tfsClient.UpdateCodeCoverageSummaryAsync(data, project, buildId, cancellationToken: cancellationToken);
            }
        }


    }
}
