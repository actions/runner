using Microsoft.TeamFoundation.TestManagement.WebApi;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public interface ICodeCoverageSummaryReader : IExtension
    {
        /// <summary>
        /// Get CodeCoverageStatistics from summary file
        /// </summary>
        /// <param name="summaryFileLocation">coverage summary file</param>
        /// <returns></returns>
        IEnumerable<CodeCoverageStatistics> GetCodeCoverageSummary(IExecutionContext context, string summaryFileLocation);

        /// <summary>
        /// Result reader name
        /// </summary>
        string Name { get; }
    }
}