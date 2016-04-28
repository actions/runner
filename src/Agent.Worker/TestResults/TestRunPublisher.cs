using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    [ServiceLocator(Default = typeof(TestRunPublisher))]
    public interface ITestRunPublisher : IAgentService
    {
        Task StartTestRunAsync(TestRunData testRunData);
        Task AddResultsAsync(TestCaseResultData[] testResults);
        TestRunData ReadResultsFromFile(string filePath);
        Task EndTestRunAsync(bool publishAttachmentsAsArchive = false);
        void InitializePublisher(IExecutionContext executionContext, VssConnection connection, string projectName, TestRunContext runContext, IResultReader resultReader);
        TestRunData ReadResultsFromFile(string filePath, string runName);
    }

    public class TestRunPublisher : AgentService, ITestRunPublisher
    {
        #region Private
        const int BATCH_SIZE = 1000;
        const int PUBLISH_TIMEOUT = 300;
        const int TCM_MAX_FILESIZE = 104857600;
        private IExecutionContext _executionContext;
        private string _projectName;
        private ITestResultsServer _testResultsServer;
        private TestRun _testRun;
        private TestRunData _testRunData;
        private IResultReader _resultReader;
        private TestRunContext _runContext;
        #endregion

        #region Public API
        public void InitializePublisher(IExecutionContext executionContext, VssConnection connection, string projectName, TestRunContext runContext, IResultReader resultReader)
        {
            Trace.Entering();
            _executionContext = executionContext;
            _projectName = projectName;
            _runContext = runContext;
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
        public async Task AddResultsAsync(TestCaseResultData[] testResults)
        {
            Trace.Entering();
            int noOfResultsToBePublished = BATCH_SIZE;

            for (int i = 0; i < testResults.Length; i += BATCH_SIZE)
            {
                if (i + BATCH_SIZE >= testResults.Length)
                {
                    noOfResultsToBePublished = testResults.Length - i;
                }
                _executionContext.Debug($"Test results remaining: {(testResults.Length - i)}");

                var currentBatch = new TestCaseResultData[noOfResultsToBePublished];
                Array.Copy(testResults, i, currentBatch, 0, noOfResultsToBePublished);

                List<TestCaseResult> testresults = await _testResultsServer.AddTestResultsToTestRunAsync(currentBatch, _projectName, _testRun.Id, _executionContext.CancellationToken);

                for (int j = 0; j < noOfResultsToBePublished; j++)
                {
                    // Do not upload duplicate entries 
                    string[] attachments = testResults[i + j].Attachments;
                    if (attachments != null)
                    {
                        Hashtable attachedFiles = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
                        foreach (string attachment in attachments)
                        {
                            if (!attachedFiles.ContainsKey(attachment))
                            {
                                TestAttachmentRequestModel reqModel = GetAttachmentRequestModel(attachment);
                                if (reqModel != null)
                                {
                                    await _testResultsServer.CreateTestResultAttachmentAsync(reqModel, _projectName, _testRun.Id, testresults[j].Id, _executionContext.CancellationToken);
                                }
                                attachedFiles.Add(attachment, null);
                            }
                        }
                    }
                    // Upload console log as attachment
                    string consoleLog = testResults[i + j].ConsoleLog;
                    TestAttachmentRequestModel attachmentRequestModel = GetConsoleLogAttachmentRequestModel(consoleLog);
                    if (attachmentRequestModel != null)
                    {
                        await _testResultsServer.CreateTestResultAttachmentAsync(attachmentRequestModel, _projectName, _testRun.Id, testresults[j].Id, _executionContext.CancellationToken);
                    }
                }
            }

            Trace.Leaving();
        }

        /// <summary>
        /// Start a test run  
        /// </summary>
        public async Task StartTestRunAsync(TestRunData testRun)
        {
            Trace.Entering();
            _testRunData = testRun;

            _testRun = await _testResultsServer.CreateTestRunAsync(_projectName, _testRunData, _executionContext.CancellationToken);
            Trace.Leaving();
        }

        /// <summary>
        /// Mark the test run as completed 
        /// </summary>
        public async Task EndTestRunAsync(bool publishAttachmentsAsArchive = false)
        {
            Trace.Entering();
            RunUpdateModel updateModel = new RunUpdateModel(
                completedDate: _testRunData.CompleteDate,
                state: TestRunState.Completed.ToString()
                );
            _testRun = await _testResultsServer.UpdateTestRunAsync(_projectName, _testRun.Id, updateModel, _executionContext.CancellationToken);

            // Uploading run level attachments, only after run is marked completed;
            // so as to make sure that any server jobs that acts on the uploaded data (like CoverAn job does for Coverage files)  
            // have a fully published test run results, in case it wants to iterate over results 
            if (publishAttachmentsAsArchive)
            {
                UploadTestRunAttachmentsAsArchive();
            }
            else
            {
                UploadTestRunAttachmentsIndividual();
            }

            _executionContext.Debug(string.Format(CultureInfo.CurrentCulture, "Published Test Run : {0}", _testRun.WebAccessUrl));
        }

        /// <summary>
        /// Converts the given results file to TestRunData object
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>TestRunData</returns>
        public TestRunData ReadResultsFromFile(string filePath)
        {
            Trace.Entering();
            return _resultReader.ReadResults(_executionContext, filePath, _runContext);
        }

        /// <summary>
        /// Converts the given results file to TestRunData object
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="runName">Run Name</param>
        /// <returns>TestRunData</returns>
        public TestRunData ReadResultsFromFile(string filePath, string runName)
        {
            Trace.Entering();
            _runContext.RunName = runName;
            return _resultReader.ReadResults(_executionContext, filePath, _runContext);
        }
        #endregion

        private void UploadTestRunAttachmentsAsArchive()
        {
            Trace.Entering();
            // Do not upload duplicate entries 
            HashSet<string> attachedFiles = UniqueTestRunFiles;
            try
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                string zipFile = Path.Combine(tempDirectory, "TestResults_" + _testRun.Id + ".zip");

                File.Delete(zipFile); //if there's already file. remove silently without exception
                CreateZipFile(zipFile, attachedFiles);
                CreateTestRunAttachment(zipFile);
            }
            catch (Exception ex)
            {
                _executionContext.Warning(StringUtil.Loc("UnableToArchiveResults", ex));
                UploadTestRunAttachmentsIndividual();
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

        private void UploadTestRunAttachmentsIndividual()
        {
            Trace.Entering();
            _executionContext.Debug("Uploading test run attachements individually");
            // Do not upload duplicate entries 
            HashSet<string> attachedFiles = UniqueTestRunFiles;
            foreach (string file in attachedFiles)
            {
                CreateTestRunAttachment(file);
            }
        }

        private void CreateTestRunAttachment(string zipFile)
        {
            Trace.Entering();
            TestAttachmentRequestModel reqModel = GetAttachmentRequestModel(zipFile);
            if (reqModel != null)
            {
                Task<TestAttachmentReference> trTask = _testResultsServer.CreateTestRunAttachmentAsync(reqModel, _projectName, _testRun.Id, _executionContext.CancellationToken);
                trTask.Wait();
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
            if (File.Exists(attachment) && new FileInfo(attachment).Length <= TCM_MAX_FILESIZE)
            {
                byte[] bytes = File.ReadAllBytes(attachment);
                string encodedData = Convert.ToBase64String(bytes);
                if (encodedData.Length <= TCM_MAX_FILESIZE)
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
                _executionContext.Warning(StringUtil.Loc("NoSpaceOnDisk", attachment));
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

        private HashSet<string> UniqueTestRunFiles
        {
            get
            {
                var attachedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_testRunData.Attachments != null)
                {
                    foreach (string attachment in _testRunData.Attachments)
                    {
                        attachedFiles.Add(attachment);
                    }
                }
                return attachedFiles;
            }
        }
    }
}