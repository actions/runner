using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class TestRunPublisherTests : IDisposable
    {
        private static Mock<IExecutionContext> _ec;
        private static string _attachmentFilePath;
        private static TestRunContext _testRunContext;
        private static ITestRunPublisher _publisher;
        private static Mock<IResultReader> _reader;
        private static Mock<ITestResultsServer> _testResultServer;
        private TestRunData _testRunData;
        private int _runId;
        private string _projectId;
        private TestRunData _testRun;
        private TestAttachmentRequestModel _attachmentRequestModel;
        private TestResultCreateModel[] _resultCreateModels;
        private Dictionary<int, List<TestAttachmentRequestModel>> _resultsLevelAttachments = new Dictionary<int, List<TestAttachmentRequestModel>>();
        private RunUpdateModel _updateProperties;
        private List<int> _batchSizes = new List<int>();
        private string _resultsFilepath;
        private TestRunContext _runContext;

        public TestRunPublisherTests()
        {
            _attachmentFilePath = "attachment.txt";

            File.WriteAllText(_attachmentFilePath, "asdf");
            _testRunContext = new TestRunContext("owner", "platform", "config", 1, "builduri", "releaseuri", "releaseenvuri");

            _reader = new Mock<IResultReader>();
            _reader.Setup(x => x.ReadResults(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<TestRunContext>()))
                        .Callback<IExecutionContext, string, TestRunContext>
                        ((executionContext, filePath, runContext) =>
                        {
                            _runContext = runContext;
                            _resultsFilepath = filePath;
                        })
                        .Returns((IExecutionContext executionContext, string filePath, TestRunContext runContext) =>
                        {
                            TestRunData trd = new TestRunData(
                                name: "xyz",
                                buildId: runContext.BuildId,
                                completedDate: "",
                                state: "InProgress",
                                isAutomated: true,
                                dueDate: "",
                                type: "",
                                buildFlavor: runContext.Configuration,
                                buildPlatform: runContext.Platform,
                                releaseUri: runContext.ReleaseUri,
                                releaseEnvironmentUri: runContext.ReleaseEnvironmentUri
                            );
                            trd.Attachments = new string[] { "attachment.txt" };
                            return trd;
                        });

            _testResultServer = new Mock<ITestResultsServer>();
            _testResultServer.Setup(x => x.InitializeServer(It.IsAny<Client.VssConnection>()));
            _testResultServer.Setup(x => x.AddTestResultsToTestRun(It.IsAny<TestResultCreateModel[]>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Callback<TestResultCreateModel[], string, int, CancellationToken>
                        ((currentBatch, projectName, testRunId, cancellationToken) =>
                        {
                            _batchSizes.Add(currentBatch.Length);
                            _resultCreateModels = currentBatch;
                        })
                        .Returns(() => Task.Factory.StartNew(() =>
                        {
                            List<TestCaseResult> resultsList = new List<TestCaseResult>();
                            int i = 0;
                            foreach (TestResultCreateModel resultCreateModel in _resultCreateModels)
                            {
                                resultsList.Add(new TestCaseResult() { Id = ++i });
                            }
                            return resultsList;
                        }));

            _testResultServer.Setup(x => x.CreateTestRun(It.IsAny<string>(), It.IsAny<RunCreateModel>(), It.IsAny<CancellationToken>()))
                        .Callback<string, RunCreateModel, CancellationToken>
                        ((projectName, testRunData, cancellationToken) =>
                        {
                            _projectId = projectName;
                            _testRun = (TestRunData)testRunData;
                        })
                        .Returns(Task.Factory.StartNew(() => { return new TestRun() { Name = "TestRun", Id = 1 }; }));

            _testResultServer.Setup(x => x.UpdateTestRun(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RunUpdateModel>(), It.IsAny<CancellationToken>()))
                        .Callback<string, int, RunUpdateModel, CancellationToken>
                        ((projectName, testRunId, updateModel, cancellationToken) =>
                        {
                            _runId = testRunId;
                            _projectId = projectName;
                            _updateProperties = updateModel;
                        })
                        .Returns(Task.Factory.StartNew(() => { return new TestRun() { Name = "TestRun", Id = 1 }; }));

            _testResultServer.Setup(x => x.CreateTestRunAttachment(
                        It.IsAny<TestAttachmentRequestModel>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Callback<TestAttachmentRequestModel, string, int, CancellationToken>
                        ((reqModel, projectName, testRunId, cancellationToken) =>
                        {
                            _attachmentRequestModel = reqModel;
                            _projectId = projectName;
                            _runId = testRunId;
                        })
                        .Returns(Task.Factory.StartNew(() => { return new TestAttachmentReference(); }));

            _testResultServer.Setup(x => x.CreateTestResultAttachment(It.IsAny<TestAttachmentRequestModel>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Callback<TestAttachmentRequestModel, string, int, int, CancellationToken>
                        ((reqModel, projectName, testRunId, testCaseResultId, cancellationToken) =>
                        {
                            if (_resultsLevelAttachments.ContainsKey(testCaseResultId))
                            {
                                _resultsLevelAttachments[testCaseResultId].Add(reqModel);
                            }
                            else
                            {
                                _resultsLevelAttachments.Add(testCaseResultId, new List<TestAttachmentRequestModel>() { reqModel });
                            }
                        })
                        .Returns(Task.Factory.StartNew(() => { return new TestAttachmentReference(); }));
        }

        [Fact]
        [Trait("Level", "L0")]
        public void ReadResultsSendsRunTitleToReader()
        {
            SetupMocks();
            _testRunData = _publisher.ReadResultsFromFile("filepath", "runName");
            Assert.Equal("runName", _runContext.RunName);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void AddResultsWithAttachmentsCallsRightApi()
        {
            SetupMocks();
            //Add results
            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData);
            TestCaseResultData result = new TestCaseResultData();
            result.Attachments = new string[] { "attachment.txt" };
            _publisher.AddResults(new TestCaseResultData[] { result }).Wait();

            Assert.Equal(_resultsLevelAttachments.Count, 1);
            Assert.Equal(_resultsLevelAttachments[1].Count, 1);
            Assert.Equal(_resultsLevelAttachments[1][0].AttachmentType, AttachmentType.GeneralAttachment.ToString());
            Assert.Equal(_resultsLevelAttachments[1][0].Comment, "");
            Assert.Equal(_resultsLevelAttachments[1][0].FileName, "attachment.txt");
        }

        [Fact]
        [Trait("Level", "L0")]
        public void TrxFileUploadsWithCorrectAttachmentType()
        {
            SetupMocks();
            File.WriteAllText("sampleTrx.trx", "asdf");
            ResetValues();

            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData).Wait();
            TestCaseResultData result = new TestCaseResultData();
            result.Attachments = new string[] { "sampleTrx.trx" };
            _publisher.AddResults(new TestCaseResultData[] { result }).Wait();
            Assert.Equal(_resultsLevelAttachments.Count, 1);
            Assert.Equal(_resultsLevelAttachments[1].Count, 1);
            Assert.Equal(_resultsLevelAttachments[1][0].AttachmentType, AttachmentType.TmiTestRunSummary.ToString());
            try
            {
                File.Delete("sampleTrx.trx");
            }
            catch
            { }
        }

        [Fact]
        [Trait("Level", "L0")]
        public void TestImpactFileUploadsWithCorrectAttachmentType()
        {
            SetupMocks();
            File.WriteAllText("testimpact.xml", "asdf");
            ResetValues();

            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData).Wait();
            TestCaseResultData result = new TestCaseResultData();
            result.Attachments = new string[] { "testimpact.xml" };
            _publisher.AddResults(new TestCaseResultData[] { result }).Wait();
            Assert.Equal(_resultsLevelAttachments.Count, 1);
            Assert.Equal(_resultsLevelAttachments[1].Count, 1);
            Assert.Equal(_resultsLevelAttachments[1][0].AttachmentType, AttachmentType.TestImpactDetails.ToString());
            try
            {
                File.Delete("testimpact.xml");
            }
            catch
            { }
        }

        [Fact]
        [Trait("Level", "L0")]
        public void SystemInformationFileUploadsWithCorrectAttachmentType()
        {
            SetupMocks();
            File.WriteAllText("SystemInformation.xml", "asdf");
            ResetValues();

            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData).Wait();
            TestCaseResultData result = new TestCaseResultData();
            result.Attachments = new string[] { "SystemInformation.xml" };
            _publisher.AddResults(new TestCaseResultData[] { result }).Wait();
            Assert.Equal(_resultsLevelAttachments.Count, 1);
            Assert.Equal(_resultsLevelAttachments[1].Count, 1);
            Assert.Equal(_resultsLevelAttachments[1][0].AttachmentType, AttachmentType.IntermediateCollectorData.ToString());
            try
            {
                File.Delete("SystemInformation.xml");
            }
            catch
            { }
        }

        [Fact]
        [Trait("Level", "L0")]
        public void StartTestRunCallsRightApi()
        {
            SetupMocks();
            _testRunData = _publisher.ReadResultsFromFile("filepath");
            Assert.Equal(_testRunContext, _runContext);
            Assert.Equal("filepath", _resultsFilepath);

            //Start run 
            _publisher.StartTestRun(_testRunData).Wait();
            Assert.Equal(_projectId, "Project1");
            Assert.Equal(_testRunData, _testRun);
            Assert.Equal(_projectId, "Project1");
            Assert.Equal(_runId, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void EndTestRunCallsRightApi()
        {
            SetupMocks();
            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData).Wait();
            _projectId = "";
            //End run.
            _publisher.EndTestRun().Wait();
            Assert.Equal(_runId, 1);
            Assert.Equal(_projectId, "Project1");
            Assert.Equal(_updateProperties.State, "Completed");
            Assert.Equal(_attachmentRequestModel.AttachmentType, AttachmentType.GeneralAttachment.ToString());
            Assert.Equal(_attachmentRequestModel.FileName, "attachment.txt");
            Assert.Equal(_attachmentRequestModel.Comment, "");
        }

        [Fact]
        [Trait("Level", "L0")]
        public void EndTestRunWithArchiveCallsRightApi()
        {
            SetupMocks();
            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData).Wait();
            _projectId = "";
            //End run.
            _publisher.EndTestRun(publishAttachmentsAsArchive: true).Wait();
            Assert.Equal(_runId, 1);
            Assert.Equal(_projectId, "Project1");
            Assert.Equal(_updateProperties.State, "Completed");
            Assert.Equal(_attachmentRequestModel.AttachmentType, AttachmentType.GeneralAttachment.ToString());
            Assert.Equal(_attachmentRequestModel.FileName, "TestResults_1.zip");
            Assert.Equal(_attachmentRequestModel.Comment, "");
        }

        [Fact]
        [Trait("Level", "L0")]
        public void BatchSizeIsCorrect()
        {
            SetupMocks();
            int batchSize = 1000;
            ResetValues();
            _publisher.StartTestRun(new TestRunData()).Wait();
            List<TestCaseResultData> testCaseResultData = new List<TestCaseResultData>();
            for (int i = 0; i < batchSize + 1; i++) { testCaseResultData.Add(new TestCaseResultData()); }
            _publisher.AddResults(testCaseResultData.ToArray()).Wait();
            Assert.Equal(2, _batchSizes.Count);
            Assert.Equal(batchSize, _batchSizes[0]);
            Assert.Equal(1, _batchSizes[1]);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void PublishConsoleLogIsSuccessful()
        {
            SetupMocks();
            ResetValues();

            TestCaseResultData testResultWithLog = new TestCaseResultData();
            testResultWithLog.ConsoleLog = "Publish console log is successfully logged";

            TestCaseResultData testResultWithNoLog = new TestCaseResultData();
            testResultWithNoLog.ConsoleLog = "";

            TestCaseResultData testResultDefault = new TestCaseResultData();

            List<TestCaseResultData> testResults = new List<TestCaseResultData>() { testResultWithLog, testResultWithNoLog, testResultDefault };

            // execute publish task
            _testRunData = _publisher.ReadResultsFromFile("filepath");
            _publisher.StartTestRun(_testRunData).Wait();
            _publisher.AddResults(testResults.ToArray()).Wait();
            _publisher.EndTestRun().Wait();

            // validate
            Assert.Equal(_resultsLevelAttachments.Count, 1);
            Assert.Equal(_resultsLevelAttachments[1].Count, 1);
            Assert.Equal(_resultsLevelAttachments[1][0].AttachmentType, AttachmentType.ConsoleLog.ToString());
            string encodedData = _resultsLevelAttachments[1][0].Stream;
            byte[] bytes = Convert.FromBase64String(encodedData);
            string decodedData = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.Equal(decodedData, testResultWithLog.ConsoleLog);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_attachmentFilePath);
            }
            catch
            { }
        }

        private void ResetValues()
        {
            _runId = 0;
            _projectId = "";
            _attachmentRequestModel = null;
            _resultCreateModels = null;
            _resultsLevelAttachments = new Dictionary<int, List<TestAttachmentRequestModel>>();
            _updateProperties = null;
            _batchSizes = new List<int>();
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);

            hc.SetSingleton<ITestResultsServer>(_testResultServer.Object);

            _publisher = new TestRunPublisher();
            _publisher.Initialize(hc);
            _publisher.InitializePublisher(_ec.Object, new Client.VssConnection(new Uri("http://dummyurl"), new Common.VssCredentials()), "Project1", _testRunContext, _reader.Object);
        }
    }
}
