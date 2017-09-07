using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    [ServiceLocator(Default = typeof(TestRunPublisher))]
    public interface ITestRunPublisher : IAgentService
    {
        void InitializePublisher(IExecutionContext executionContext, VssConnection connection, string projectName, IResultReader resultReader);
        Task<TestRun> StartTestRunAsync(TestRunData testRunData, CancellationToken cancellationToken = default(CancellationToken));
        Task AddResultsAsync(TestRun testRun, TestCaseResultData[] testResults, CancellationToken cancellationToken = default(CancellationToken));
        Task EndTestRunAsync(TestRunData testRunData, int testRunId, bool publishAttachmentsAsArchive = false, CancellationToken cancellationToken = default(CancellationToken));
        TestRunData ReadResultsFromFile(TestRunContext runContext, string filePath, string runName);
        TestRunData ReadResultsFromFile(TestRunContext runContext, string filePath);
    }

    public class TestRunPublisher : AgentService, ITestRunPublisher
    {
        #region Private
        const int BATCH_SIZE = 1000;
        const int PUBLISH_TIMEOUT = 300;
        const int TCM_MAX_FILECONTENT_SIZE = 100 * 1024 * 1024; //100 MB
        const int TCM_MAX_FILESIZE = 75 * 1024 * 1024; // 75 MB
        private IExecutionContext _executionContext;
        private string _projectName;
        private ITestResultsServer _testResultsServer;
        private IResultReader _resultReader;
        #endregion

        #region Public API
        public void InitializePublisher(IExecutionContext executionContext, VssConnection connection, string projectName, IResultReader resultReader)
        {
            Trace.Entering();
            _executionContext = executionContext;
            _projectName = projectName;
            _resultReader = resultReader;
            connection.InnerHandler.Settings.SendTimeout = TimeSpan.FromSeconds(PUBLISH_TIMEOUT);
            _testResultsServer = HostContext.GetService<ITestResultsServer>();
            _testResultsServer.InitializeServer(connection);
            Trace.Leaving();
        }

        /// <summary>
        /// Publishes the given results to the test run.
        /// </summary>
        /// <param name="testResults">Results to be published.</param>
        public async Task AddResultsAsync(TestRun testRun, TestCaseResultData[] testResults, CancellationToken cancellationToken)
        {
            Trace.Entering();
            int noOfResultsToBePublished = BATCH_SIZE;

            _executionContext.Output(StringUtil.Loc("PublishingTestResults", testRun.Id));

            for (int i = 0; i < testResults.Length; i += BATCH_SIZE)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (i + BATCH_SIZE >= testResults.Length)
                {
                    noOfResultsToBePublished = testResults.Length - i;
                }
                _executionContext.Output(StringUtil.Loc("TestResultsRemaining", (testResults.Length - i), testRun.Id));

                var currentBatch = new TestCaseResultData[noOfResultsToBePublished];
                Array.Copy(testResults, i, currentBatch, 0, noOfResultsToBePublished);

                List<TestCaseResult> testresults = await _testResultsServer.AddTestResultsToTestRunAsync(currentBatch, _projectName, testRun.Id, cancellationToken);

                for (int j = 0; j < noOfResultsToBePublished; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Do not upload duplicate entries
                    string[] attachments = testResults[i + j].Attachments;
                    if (attachments != null)
                    {
                        Hashtable attachedFiles = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
                        var createAttachmentsTasks = attachments.Select(async attachment =>
                        {
                            if (!attachedFiles.ContainsKey(attachment))
                            {
                                TestAttachmentRequestModel reqModel = GetAttachmentRequestModel(attachment);
                                if (reqModel != null)
                                {
                                    await _testResultsServer.CreateTestResultAttachmentAsync(reqModel, _projectName, testRun.Id, testresults[j].Id, cancellationToken);
                                }
                                attachedFiles.Add(attachment, null);
                            }
                        });
                        await Task.WhenAll(createAttachmentsTasks);
                    }

                    // Upload console log as attachment
                    string consoleLog = testResults[i + j].ConsoleLog;
                    TestAttachmentRequestModel attachmentRequestModel = GetConsoleLogAttachmentRequestModel(consoleLog);
                    if (attachmentRequestModel != null)
                    {
                        await _testResultsServer.CreateTestResultAttachmentAsync(attachmentRequestModel, _projectName, testRun.Id, testresults[j].Id, cancellationToken);
                    }

                    // Upload standard error as attachment
                    string standardError = testResults[i + j].StandardError;
                    TestAttachmentRequestModel stdErrAttachmentRequestModel = GetStandardErrorAttachmentRequestModel(standardError);
                    if (stdErrAttachmentRequestModel != null)
                    {
                        await _testResultsServer.CreateTestResultAttachmentAsync(stdErrAttachmentRequestModel, _projectName, testRun.Id, testresults[j].Id, cancellationToken);
                    }
                }
            }

            Trace.Leaving();
        }

        /// <summary>
        /// Start a test run
        /// </summary>
        public async Task<TestRun> StartTestRunAsync(TestRunData testRunData, CancellationToken cancellationToken)
        {
            Trace.Entering();

            var testRun = await _testResultsServer.CreateTestRunAsync(_projectName, testRunData, cancellationToken);
            Trace.Leaving();
            return testRun;
        }

        /// <summary>
        /// Mark the test run as completed
        /// </summary>
        public async Task EndTestRunAsync(TestRunData testRunData, int testRunId, bool publishAttachmentsAsArchive = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            Trace.Entering();
            RunUpdateModel updateModel = new RunUpdateModel(
                completedDate: testRunData.CompleteDate,
                state: TestRunState.Completed.ToString()
                );
            TestRun testRun = await _testResultsServer.UpdateTestRunAsync(_projectName, testRunId, updateModel, cancellationToken);

            // Uploading run level attachments, only after run is marked completed;
            // so as to make sure that any server jobs that acts on the uploaded data (like CoverAn job does for Coverage files)
            // have a fully published test run results, in case it wants to iterate over results

            if (publishAttachmentsAsArchive)
            {
                await UploadTestRunAttachmentsAsArchiveAsync(testRunId, testRunData.Attachments, cancellationToken);
            }
            else
            {
                await UploadTestRunAttachmentsIndividualAsync(testRunId, testRunData.Attachments, cancellationToken);
            }

            _executionContext.Output(string.Format(CultureInfo.CurrentCulture, "Published Test Run : {0}", testRun.WebAccessUrl));
        }

        /// <summary>
        /// Converts the given results file to TestRunData object
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>TestRunData</returns>
        public TestRunData ReadResultsFromFile(TestRunContext runContext, string filePath)
        {
            Trace.Entering();
            return _resultReader.ReadResults(_executionContext, filePath, runContext);
        }

        /// <summary>
        /// Converts the given results file to TestRunData object
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="runName">Run Name</param>
        /// <returns>TestRunData</returns>
        public TestRunData ReadResultsFromFile(TestRunContext runContext, string filePath, string runName)
        {
            Trace.Entering();
            runContext.RunName = runName;
            return _resultReader.ReadResults(_executionContext, filePath, runContext);
        }
        #endregion

        private async Task UploadTestRunAttachmentsAsArchiveAsync(int testRunId, string[] attachments, CancellationToken cancellationToken)
        {
            Trace.Entering();
            // Do not upload duplicate entries
            HashSet<string> attachedFiles = GetUniqueTestRunFiles(attachments);
            try
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                string zipFile = Path.Combine(tempDirectory, "TestResults_" + testRunId + ".zip");

                File.Delete(zipFile); //if there's already file. remove silently without exception
                CreateZipFile(zipFile, attachedFiles);
                await CreateTestRunAttachmentAsync(testRunId, zipFile, cancellationToken);
            }
            catch (Exception ex)
            {
                _executionContext.Warning(StringUtil.Loc("UnableToArchiveResults", ex));
                await UploadTestRunAttachmentsIndividualAsync(testRunId, attachments, cancellationToken);
            }
        }

        private void CreateZipFile(string zipfileName, IEnumerable<string> files)
        {
            Trace.Entering();
            // Create and open a new ZIP file
            using (ZipArchive zip = ZipFile.Open(zipfileName, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    // Add the entry for each file
                    zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                }
            }
        }

        private async Task UploadTestRunAttachmentsIndividualAsync(int testRunId, string[] attachments, CancellationToken cancellationToken)
        {
            Trace.Entering();
            _executionContext.Debug("Uploading test run attachements individually");
            // Do not upload duplicate entries
            HashSet<string> attachedFiles = GetUniqueTestRunFiles(attachments);
            var attachFilesTasks = attachedFiles.Select(async file =>
             {
                 await CreateTestRunAttachmentAsync(testRunId, file, cancellationToken);
             });
            await Task.WhenAll(attachFilesTasks);
        }

        private async Task CreateTestRunAttachmentAsync(int testRunId, string zipFile, CancellationToken cancellationToken)
        {
            Trace.Entering();
            TestAttachmentRequestModel reqModel = GetAttachmentRequestModel(zipFile);
            if (reqModel != null)
            {
                await _testResultsServer.CreateTestRunAttachmentAsync(reqModel, _projectName, testRunId, cancellationToken);
            }
        }

        private string GetAttachmentType(string file)
        {
            Trace.Entering();
            string fileName = Path.GetFileNameWithoutExtension(file);

            if (string.Compare(Path.GetExtension(file), ".coverage", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AttachmentType.CodeCoverage.ToString();
            }
            else if (string.Compare(Path.GetExtension(file), ".trx", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AttachmentType.TmiTestRunSummary.ToString();
            }
            else if (string.Compare(fileName, "testimpact", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AttachmentType.TestImpactDetails.ToString();
            }
            else if (string.Compare(fileName, "SystemInformation", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AttachmentType.IntermediateCollectorData.ToString();
            }
            else
            {
                return AttachmentType.GeneralAttachment.ToString();
            }
        }

        private TestAttachmentRequestModel GetAttachmentRequestModel(string attachment)
        {
            Trace.Entering();
            if (!File.Exists(attachment))
            {
                _executionContext.Warning(StringUtil.Loc("TestAttachmentNotExists", attachment));
                return null;
            }

            // https://stackoverflow.com/questions/13378815/base64-length-calculation
            if (new FileInfo(attachment).Length <= TCM_MAX_FILESIZE)
            {
                byte[] bytes = File.ReadAllBytes(attachment);
                string encodedData = Convert.ToBase64String(bytes);
                if (encodedData.Length <= TCM_MAX_FILECONTENT_SIZE)
                {
                    return new TestAttachmentRequestModel(encodedData, Path.GetFileName(attachment), "", GetAttachmentType(attachment));
                }
                else
                {
                    _executionContext.Warning(StringUtil.Loc("AttachmentExceededMaximum", attachment));
                }
            }
            else
            {
                _executionContext.Warning(StringUtil.Loc("AttachmentExceededMaximum", attachment));
            }

            return null;
        }

        private TestAttachmentRequestModel GetConsoleLogAttachmentRequestModel(string consoleLog)
        {
            Trace.Entering();
            if (!string.IsNullOrWhiteSpace(consoleLog))
            {
                string consoleLogFileName = "Standard Console Output.log";

                if (consoleLog.Length <= TCM_MAX_FILESIZE)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(consoleLog);
                    string encodedData = Convert.ToBase64String(bytes);
                    return new TestAttachmentRequestModel(encodedData, consoleLogFileName, "",
                        AttachmentType.ConsoleLog.ToString());
                }
                else
                {
                    _executionContext.Warning(StringUtil.Loc("AttachmentExceededMaximum", consoleLogFileName));
                }
            }

            return null;
        }

        private TestAttachmentRequestModel GetStandardErrorAttachmentRequestModel(string stdErr)
        {
            Trace.Entering();
            if (string.IsNullOrWhiteSpace(stdErr) == false)
            {
                const string stdErrFileName = "Standard_Error_Output.log";

                if (stdErr.Length <= TCM_MAX_FILESIZE)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(stdErr);
                    string encodedData = Convert.ToBase64String(bytes);
                    return new TestAttachmentRequestModel(encodedData, stdErrFileName, "",
                        AttachmentType.ConsoleLog.ToString());
                }
                else
                {
                    _executionContext.Warning(StringUtil.Loc("AttachmentExceededMaximum", stdErrFileName));
                }
            }

            return null;
        }

        private HashSet<string> GetUniqueTestRunFiles(string[] attachments)
        {
            var attachedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (attachments != null)
            {
                foreach (string attachment in attachments)
                {
                    attachedFiles.Add(attachment);
                }
            }
            return attachedFiles;
        }
    }
}