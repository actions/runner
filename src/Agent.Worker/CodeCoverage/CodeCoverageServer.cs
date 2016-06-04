using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Client;
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
        Task PublishCoverageSummaryAsync(VssConnection connection, string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken);
    }

    internal sealed class CodeCoverageServer : AgentService, ICodeCoverageServer
    {
        public async Task PublishCoverageSummaryAsync(VssConnection connection, string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken)
        {
            var testHttpClient = connection.GetClient<TestManagementHttpClient>();
            // <todo: Bug 402783> We are currently passing BuildFlavor and BuildPlatform = "" There value are required be passed to command
            CodeCoverageData data = new CodeCoverageData()
            {
                BuildFlavor = "",
                BuildPlatform = "",
                CoverageStats = coverageData.ToList()
            };

            await testHttpClient.UpdateCodeCoverageSummaryAsync(data, project, buildId, cancellationToken: cancellationToken);
        }
    }
}
