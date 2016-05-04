using Microsoft.TeamFoundation.DistributedTask.WebApi;
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
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class ResultsCommandTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private Mock<ITestRunPublisher> _mockTestRunPublisher;

        public ResultsCommandTests()
        {
            _mockTestRunPublisher = new Mock<ITestRunPublisher>();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_NullTestRunner()
        {
            SetupMocks();
            var resultCommand = new ResultsCommands();
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "ResultFile.txt");
            resultCommand.ProcessCommand(_ec.Object, command);
            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
            Assert.Equal(StringUtil.Loc("FailedToPublishTestResults", StringUtil.Loc("ArgumentNeeded", "Testrunner")), _warnings[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_InvalidTestRunner()
        {
            SetupMocks();
            var resultCommand = new ResultsCommands();
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "ResultFile.txt");
            command.Properties.Add("type", "MyTestRunner");
            resultCommand.ProcessCommand(_ec.Object, command);

            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
            Assert.Equal(StringUtil.Loc("FailedToPublishTestResults", "Unknown Test Runner."), _warnings[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_NullTestResultFiles()
        {
            SetupMocks();
            var resultCommand = new ResultsCommands();
            var command = new Command("results", "publish");
            resultCommand.ProcessCommand(_ec.Object, command);

            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
            Assert.Equal(StringUtil.Loc("FailedToPublishTestResults", StringUtil.Loc("ArgumentNeeded", "TestResults")), _warnings[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_NoTestResultFile()
        {
            SetupMocks();
            var resultCommand = new ResultsCommands();
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "NoFiles.txt");
            command.Properties.Add("type", "JUnit");
            resultCommand.ProcessCommand(_ec.Object, command);

            Assert.Equal(0, _errors.Count());
            Assert.Equal(1, _warnings.Count());
            Assert.Equal(StringUtil.Loc("FailedToPublishTestResults", "Could not find file 'NoFiles.txt'"), _warnings[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_InvalidJUnitResultFile()
        {
            SetupMocks();

            string jUnitFilePath = "JUnitSampleResults.txt";
            File.WriteAllText(jUnitFilePath, "badformat", Encoding.UTF8);

            var resultCommand = new ResultsCommands();
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
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Publish_InvalidNUnitResultFile()
        {
            SetupMocks();

            string jUnitFilePath = "NUnitSampleResults.txt";
            File.WriteAllText(jUnitFilePath, "badformat", Encoding.UTF8);

            var resultCommand = new ResultsCommands();
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
        public void VerifyResultsAreMergedWhenPublishingToSingleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommands();
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.TrueString);
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };

            var testRunData = new TestRunData();
            testRunData.Results = new TestCaseResultData[] { new TestCaseResultData(), new TestCaseResultData() };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };

            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>()))
                .Callback((TestRunData trd) =>
                {
                    Assert.Equal(resultsFiles.Count * testRunData.Attachments.Length, trd.Attachments.Length);
                });
            _mockTestRunPublisher.Setup(q => q.AddResultsAsync(It.IsAny<TestCaseResultData[]>()))
                .Callback((TestCaseResultData[] tcrd) =>
                {
                    Assert.Equal(resultsFiles.Count * testRunData.Results.Length, tcrd.Length);
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<string>()))
                .Returns(testRunData);
            _mockTestRunPublisher.Setup(q => q.EndTestRunAsync(false))
                .Returns(Task.CompletedTask);

            resultCommand.ProcessCommand(_ec.Object, command);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyStartEndTestRunTimeWhenPublishingToSingleTestRun()
        {
            SetupMocks();
            var resultCommand = new ResultsCommands();
            var command = new Command("results", "publish");
            command.Properties.Add("resultFiles", "file1.trx,file2.trx");
            command.Properties.Add("type", "NUnit");
            command.Properties.Add("mergeResults", bool.TrueString);
            var resultsFiles = new List<string> { "file1.trx", "file2.trx" };
            var durationInMs = 10;
            var testRunData = new TestRunData();
            var testCaseResultData = new TestCaseResultData();
            testCaseResultData.DurationInMs = durationInMs.ToString();

            testRunData.Results = new TestCaseResultData[] { testCaseResultData, testCaseResultData };
            testRunData.Attachments = new string[] { "attachment1", "attachment2" };

            _mockTestRunPublisher.Setup(q => q.StartTestRunAsync(It.IsAny<TestRunData>()))
                .Callback((TestRunData trd) =>
                {
                    var startedDate = DateTime.Parse(trd.StartDate, null, DateTimeStyles.RoundtripKind);
                    var endedDate = DateTime.Parse(trd.CompleteDate, null, DateTimeStyles.RoundtripKind);
                    Assert.Equal(resultsFiles.Count * testRunData.Results.Length * durationInMs, (endedDate - startedDate).TotalMilliseconds);
                });
            _mockTestRunPublisher.Setup(q => q.AddResultsAsync(It.IsAny<TestCaseResultData[]>()))
                .Callback((TestCaseResultData[] tcrd) =>
                {
                });
            _mockTestRunPublisher.Setup(q => q.ReadResultsFromFile(It.IsAny<string>()))
                .Returns(testRunData);
            _mockTestRunPublisher.Setup(q => q.EndTestRunAsync(false))
                .Returns(Task.CompletedTask);

            resultCommand.ProcessCommand(_ec.Object, command);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            _warnings = new List<string>();
            TestHostContext hc = new TestHostContext(this, name);
            hc.SetSingleton<ITestRunPublisher>(_mockTestRunPublisher.Object);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);
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
