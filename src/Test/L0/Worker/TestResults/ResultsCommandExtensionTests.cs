using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public sealed class ResultsCommandTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private Mock<IResultReader> _mockResultReader;
        private Mock<ITestRunPublisher> _mockTestRunPublisher;
        private Mock<IExtensionManager> _mockExtensionManager;
        private Mock<IAsyncCommandContext> _mockCommandContext;
        private TestHostContext _hc;
        private Variables _variables;

        public ResultsCommandTests()
        {
            _mockTestRunPublisher = new Mock<ITestRunPublisher>();

            var testRunData = new TestRunData { };
            _mockResultReader = new Mock<IResultReader>();
            _mockResultReader.Setup(x => x.Name).Returns("mockResults");
            _mockResultReader.Setup(x => x.ReadResults(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<TestRunContext>()))
                .Returns(testRunData);

            _mockTestRunPublisher = new Mock<ITestRunPublisher>();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_NullTestRunner()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "ResultFile.txt");
            Assert.Throws<ArgumentException>(() => resultCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_InvalidTestRunner()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "ResultFile.txt");
            command.Properties.Add("type", "MyTestRunner");
            Assert.Throws<ArgumentException>(() => resultCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_NullTestResultFiles()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            Assert.Throws<ArgumentException>(() => resultCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_NonExistingTestResultFile()
        {
            _mockResultReader.Setup(x => x.ReadResults(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<TestRunContext>()))
               .Throws(new IOException("Could not find file 'nonexisting.file'"));
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "nonexisting.file");
            command.Properties.Add("type", "mockResults");
            resultCommand.ProcessCommand(_ec.Object, command);
            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
            Assert.Equal(StringUtil.Loc("InvalidResultFiles", "Could not find file 'nonexisting.file'"), _warnings[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_InvalidJUnitResultFile()
        {
            SetupMocks();

            string jUnitFilePath = "JUnitSampleResults.txt";
            File.WriteAllText(jUnitFilePath, "badformat", Encoding.UTF8);

            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", jUnitFilePath);
            command.Properties.Add("type", "JUnit");

            try
            {
                resultCommand.ProcessCommand(_ec.Object, command);
            }
            finally
            {
                File.Delete(jUnitFilePath);
            }

            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
            Assert.Equal(StringUtil.Loc("InvalidResultFiles", jUnitFilePath, "JUnit"), _warnings[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_InvalidNUnitResultFile()
        {
            SetupMocks();

            string jUnitFilePath = "NUnitSampleResults.txt";
            File.WriteAllText(jUnitFilePath, "badformat", Encoding.UTF8);

            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", jUnitFilePath);
            command.Properties.Add("type", "NUnit");

            try
            {
                resultCommand.ProcessCommand(_ec.Object, command);
            }
            finally
            {
                File.Delete(jUnitFilePath);
            }

            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_DataIsHonoredWhenTestResultsFieldIsNotSpecified()
        {
            SetupMocks();

            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("type", "NUnit");
            command.Data = "testfile1,testfile2";
            resultCommand.ProcessCommand(_ec.Object, command);

            Assert.Equal(0, _errors.Count());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyResultsAreMergedWhenPublishingToSingleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.TrueString);
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData(), new TestCaseResultData() };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };

            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    Assert.Equal(resultsFiles.Count * testRunData.Attachments.Length, trd.Attachments.Length);
                });
            _mockTestRunPublisher.Setup(q => q.AddResultsAsync(It.IsAny<TestRun>(), It.IsAny<TestCaseResultData[]>(), It.IsAny<CancellationToken>()))
                .Callback((TestRun testRun, TestCaseResultData[] tcrd, CancellationToken cancellationToken) =>
                {
                    Assert.Equal(resultsFiles.Count * testRunData.Results.Length, tcrd.Length);
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>()))
                .Returns(testRunData);
            _mockTestRunPublisher.Setup(q => q.EndTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            resultCommand.ProcessCommand(_ec.Object, command);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunSystemPropertyIsSentWhenPublishingToSignleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.TrueString);
            command.Properties.Add("testRunSystem", "MAVEN");
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData(), new TestCaseResultData() };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };

            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    Assert.NotNull(trd.CustomTestFields);
                    Assert.NotEmpty(trd.CustomTestFields);
                    Assert.Equal("testRunSystem", trd.CustomTestFields[0].FieldName);
                    Assert.Equal("MAVEN", trd.CustomTestFields[0].Value);
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>()))
                .Returns(testRunData);

            resultCommand.ProcessCommand(_ec.Object, command);

            // Making sure that the callback is called.
            _mockTestRunPublisher.Verify(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunTitleIsModifiedWhenPublishingToMultipleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.FalseString);
            command.Properties.Add("runTitle", "TestRunTitle");
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData(), new TestCaseResultData() };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };
            int counter = 0;
            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    Assert.Equal(StringUtil.Format("{0}_{1}", "TestRunTitle", ++counter), trd.Name);
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testRunData);

            resultCommand.ProcessCommand(_ec.Object, command);

            // Making sure that the callback is called.
            _mockTestRunPublisher.Verify(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunTitleShouldNotBeModifiedWhenPublishingToSingleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.TrueString);
            command.Properties.Add("runTitle", "TestRunTitle");
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData(), new TestCaseResultData() };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };
            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    Assert.Equal("TestRunTitle", trd.Name);
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>()))
                .Returns(testRunData);

            resultCommand.ProcessCommand(_ec.Object, command);

            // Making sure that the callback is called.
            _mockTestRunPublisher.Verify(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunTitleShouldNotBeModifiedWhenWhenOnlyOneResultFileIsPublished()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx");
            command.Properties.Add("type", "NUnit");
            // Explicitly not merging it to check if the test run title is not modified when there's only one test file.
            command.Properties.Add("mergeResults", bool.FalseString);
            command.Properties.Add("runTitle", "TestRunTitle");
            var resultsFiles = new List<string> { "file1.trx"};

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData()};
            testRunData.Attachments = new string[] { "attachment1" };
            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    Assert.Equal("TestRunTitle", trd.Name);
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testRunData);

            resultCommand.ProcessCommand(_ec.Object, command);

            // Making sure that the callback is called.
            _mockTestRunPublisher.Verify(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunSystemPropertyIsSentWhenPublishingToTestRunPerFile()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.FalseString);
            command.Properties.Add("testRunSystem", "MAVEN");
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData(), new TestCaseResultData() };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };

            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    Assert.NotNull(trd.CustomTestFields);
                    Assert.NotEmpty(trd.CustomTestFields);
                    Assert.Equal("testRunSystem", trd.CustomTestFields[0].FieldName);
                    Assert.Equal("MAVEN", trd.CustomTestFields[0].Value);
                });

            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testRunData);
            resultCommand.ProcessCommand(_ec.Object, command);

            // There should be two calls to startestrun
            _mockTestRunPublisher.Verify(q=>q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyStartEndTestRunTimeWhenPublishingToSingleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommandExtension();
            resultCommand.Initialize(_hc);
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.TrueString);
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };
            var durationInMs = 10;
            var testRunData = new TestRunData();
            var testCaseResultData = new TestCaseResultData();
            testCaseResultData.DurationInMs = durationInMs;

            testRunData.Results = new TestCaseResultData[] { testCaseResultData, testCaseResultData };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };

            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<CancellationToken>()))
                .Callback((TestRunData trd, CancellationToken cancellationToken) =>
                {
                    var startedDate = DateTime.Parse(trd.StartDate, null, DateTimeStyles.RoundtripKind);
                    var endedDate = DateTime.Parse(trd.CompleteDate, null, DateTimeStyles.RoundtripKind);
                    Assert.Equal(resultsFiles.Count * testRunData.Results.Length * durationInMs, (endedDate - startedDate).TotalMilliseconds);
                });
            _mockTestRunPublisher.Setup(q => q.AddResultsAsync(It.IsAny<TestRun>(), It.IsAny<TestCaseResultData[]>(), It.IsAny<CancellationToken>()))
                .Callback((TestRun testRun, TestCaseResultData[] tcrd, CancellationToken cancellationToken) =>
                {
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<TestRunContext>(), It.IsAny<string>()))
                .Returns(testRunData);
            _mockTestRunPublisher.Setup(q => q.EndTestRunAsync(It.IsAny<TestRunData>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            resultCommand.ProcessCommand(_ec.Object, command);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            _hc = new TestHostContext(this, name);
            _hc.SetSingleton(_mockResultReader.Object);

            _mockExtensionManager = new Mock<IExtensionManager>();
            _mockExtensionManager.Setup(x => x.GetExtensions<IResultReader>()).Returns(new List<IResultReader> { _mockResultReader.Object, new JUnitResultReader(), new NUnitResultReader() });
            _hc.SetSingleton(_mockExtensionManager.Object);

            _hc.SetSingleton(_mockTestRunPublisher.Object);

            _mockCommandContext = new Mock<IAsyncCommandContext>();
            _hc.EnqueueInstance(_mockCommandContext.Object);

            var endpointAuthorization = new EndpointAuthorization()
            {
                Scheme = EndpointAuthorizationSchemes.OAuth
            };
            List<string> warnings;
            _variables = new Variables(_hc, new Dictionary<string, VariableValue>(), out warnings);
            _variables.Set("build.buildId", "1");
            endpointAuthorization.Parameters[EndpointAuthorizationParameters.AccessToken] = "accesstoken";

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Endpoints).Returns(new List<ServiceEndpoint> { new ServiceEndpoint { Url = new Uri("http://dummyurl"), Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = endpointAuthorization } });
            _ec.Setup(x => x.Variables).Returns(_variables);
            var asyncCommands = new List<IAsyncCommandContext>();
            _ec.Setup(x => x.AsyncCommands).Returns(asyncCommands);
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>()))
            .Callback<Issue>
            ((issue) =>
            {
                if (issue.Type == IssueType.Warning)
                {
                    _warnings.Add(issue.Message);
                }
                else if (issue.Type == IssueType.Error)
                {
                    _errors.Add(issue.Message);
                }
            });
        }
    }
}
