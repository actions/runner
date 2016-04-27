namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public interface IResultReader : IExtension
    {
        /// <summary>
        /// Reads a test results file from disk, converts it into a TestCaseResultData array   
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>TestCaseResultData Array</returns>
        TestRunData ReadResults(IExecutionContext executionContext, string filePath, TestRunContext runContext = null);

        /// <summary>
        /// Should the run level attachments be uploaded
        /// </summary>
        bool AddResultsFileToRunLevelAttachments { get; set; }

        /// <summary>
        /// Result reader name
        /// </summary>
        string Name { get; }
    }
}