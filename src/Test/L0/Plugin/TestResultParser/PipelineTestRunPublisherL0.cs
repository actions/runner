using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Plugins.Log.TestResultParser.Plugin;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.TestResults.WebApi;
using Moq;
using Xunit;
using TestOutcome = Agent.Plugins.Log.TestResultParser.Contracts.TestOutcome;
using TestRun = Agent.Plugins.Log.TestResultParser.Contracts.TestRun;

namespace Test.L0.Plugin.TestResultParser
{
    public class PipelineTestRunPublisherL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task PipelineTestRunPublisher_PublishTestRun()
        {
            var clientFactory = new Mock<IClientFactory>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testClient = new Mock<TestResultsHttpClient>(new Uri("http://dummyurl"), new VssCredentials());
            var pipelineConfig = new PipelineConfig()
            {
                BuildId = 1,
                Project = new Guid()
            };

            clientFactory.Setup(x => x.GetClient<TestResultsHttpClient>()).Returns(testClient.Object);
            testClient.Setup(x =>
                x.CreateTestRunAsync(It.IsAny<RunCreateModel>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Microsoft.TeamFoundation.TestManagement.WebApi.TestRun()));
            testClient.Setup(x =>
                    x.AddTestResultsToTestRunAsync(It.IsAny<TestCaseResult[]>(), It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<TestCaseResult>()));
            testClient.Setup(x =>
                    x.UpdateTestRunAsync(It.IsAny<RunUpdateModel>(), It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Microsoft.TeamFoundation.TestManagement.WebApi.TestRun()));

            var publisher = new PipelineTestRunPublisher(clientFactory.Object, pipelineConfig, logger.Object, telemetry.Object);
            await publisher.PublishAsync(new TestRun("FakeTestResultParser/1", "Fake", 1)
            {
                PassedTests = new List<TestResult>()
                {
                    new TestResult()
                    {
                        Name = "pass",
                        Outcome = TestOutcome.Passed
                    }
                }
            });

            testClient.Verify(x =>
                x.CreateTestRunAsync(It.Is<RunCreateModel>(run => run.Name.Equals("Fake test run 1 - automatically inferred results", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()));
            testClient.Verify(x => x.AddTestResultsToTestRunAsync(It.Is<TestCaseResult[]>(res => res.Length == 1),
                It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()));
            testClient.Verify(x => x.UpdateTestRunAsync(It.Is<RunUpdateModel>(run => run.State.Equals("completed", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task PipelineTestRunPublisher_PublishTestRun_ValidateTestResults()
        {
            var clientFactory = new Mock<IClientFactory>();
            var logger = new Mock<ITraceLogger>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var testClient = new Mock<TestResultsHttpClient>(new Uri("http://dummyurl"), new VssCredentials());
            var pipelineConfig = new PipelineConfig()
            {
                BuildId = 1,
                Project = new Guid()
            };

            clientFactory.Setup(x => x.GetClient<TestResultsHttpClient>()).Returns(testClient.Object);
            testClient.Setup(x =>
                x.CreateTestRunAsync(It.IsAny<RunCreateModel>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Microsoft.TeamFoundation.TestManagement.WebApi.TestRun()
                {
                    Id = 1
                }));
            testClient.Setup(x =>
                    x.AddTestResultsToTestRunAsync(It.IsAny<TestCaseResult[]>(), It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<TestCaseResult>()));
            testClient.Setup(x =>
                    x.UpdateTestRunAsync(It.IsAny<RunUpdateModel>(), It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Microsoft.TeamFoundation.TestManagement.WebApi.TestRun()
                {
                    Id = 1
                }));

            var publisher = new PipelineTestRunPublisher(clientFactory.Object, pipelineConfig, logger.Object, telemetry.Object);
            await publisher.PublishAsync(new TestRun("FakeTestResultParser/1", "Fake", 1)
            {
                PassedTests = new List<TestResult>()
                {
                    new TestResult()
                    {
                        Name = "pass",
                        Outcome = TestOutcome.Passed,
                        ExecutionTime = TimeSpan.FromSeconds(2)
                    }
                },
                FailedTests = new List<TestResult>()
                {
                    new TestResult()
                    {
                        Name = "fail",
                        Outcome = TestOutcome.Failed,
                        StackTrace = "exception",
                        ExecutionTime = TimeSpan.Zero
                    }
                },
                SkippedTests = new List<TestResult>()
                {
                    new TestResult()
                    {
                        Name = "skip",
                        Outcome = TestOutcome.NotExecuted
                    }
                },
            });

            testClient.Verify(x =>
                x.CreateTestRunAsync(It.IsAny<RunCreateModel>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()));
            testClient.Verify(x => x.AddTestResultsToTestRunAsync(It.Is<TestCaseResult[]>(res => res.Length == 3
                                                                                                 && ValidateResult(res[0], TestOutcome.Passed)
                                                                                                 && ValidateResult(res[1], TestOutcome.Failed)
                                                                                                 && ValidateResult(res[2], TestOutcome.NotExecuted)),
                It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()));
            testClient.Verify(x => x.UpdateTestRunAsync(It.Is<RunUpdateModel>(run => run.State.Equals("completed", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Guid>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()));
        }

        private bool ValidateResult(TestCaseResult result, TestOutcome outcome)
        {
            switch (outcome)
            {
                case TestOutcome.Passed:
                    return result.AutomatedTestName.Equals("pass") &&
                        result.TestCaseTitle.Equals("pass") &&
                        result.Outcome.Equals("passed", StringComparison.OrdinalIgnoreCase) &&
                        result.DurationInMs == TimeSpan.FromSeconds(2).TotalMilliseconds;
                case TestOutcome.Failed:
                    return result.AutomatedTestName.Equals("fail") &&
                           result.TestCaseTitle.Equals("fail") &&
                           result.Outcome.Equals("failed", StringComparison.OrdinalIgnoreCase) &&
                           result.DurationInMs == TimeSpan.FromSeconds(0).TotalMilliseconds &&
                           result.StackTrace.Equals("exception");
                case TestOutcome.NotExecuted:
                    return result.AutomatedTestName.Equals("skip") &&
                           result.TestCaseTitle.Equals("skip") &&
                           result.Outcome.Equals("notexecuted", StringComparison.OrdinalIgnoreCase) &&
                           result.DurationInMs == TimeSpan.FromSeconds(0).TotalMilliseconds;
            }

            return false;
        }
    }
}
