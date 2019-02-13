using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Plugins.Log.TestResultParser.Plugin;
using Moq;
using Xunit;
using ITestRunPublisher = Agent.Plugins.Log.TestResultParser.Contracts.ITestRunPublisher;

namespace Test.L0.Plugin.TestResultParser
{
    public class TestRunManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);
            var fakeRun = new TestRun("mocha/1", "somename", 1)
            {
                TestRunSummary = new TestRunSummary
                {
                    TotalPassed = 5,
                    TotalSkipped = 1,
                    TotalFailed = 1,
                    TotalExecutionTime = TimeSpan.FromMinutes(1),
                    TotalTests = 7
                }
            };

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>())).Returns(Task.FromResult(GetFakePipelineTestRun(fakeRun, 1)));

            await runManager.PublishAsync(fakeRun);

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun_TestRunIsNotValid()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>()));

            await runManager.PublishAsync(null);

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()), Times.Never());
            logger.Verify(x => x.Error(It.IsAny<string>()));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun_TestSummaryIsNotValid()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>()));

            await runManager.PublishAsync(new TestRun("fake/1", "somename", 1)
            {
                TestRunSummary = null
            });

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()), Times.Never());
            logger.Verify(x => x.Error(It.IsAny<string>()));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun_TestSummaryWithoutTests()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>()));

            await runManager.PublishAsync(new TestRun("fake/1", "somename", 1));

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()), Times.Never());
            logger.Verify(x => x.Error(It.IsAny<string>()));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun_TotalTestsLessThanActual()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);
            var fakeRun = new TestRun("fake/1", "somename", 1)
            {
                TestRunSummary = new TestRunSummary
                {
                    TotalPassed = 5,
                    TotalSkipped = 1,
                    TotalFailed = 1,
                    TotalExecutionTime = TimeSpan.FromMinutes(1),
                    TotalTests = 6
                }
            };

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>())).Returns(Task.FromResult(GetFakePipelineTestRun(fakeRun, 1)));

            await runManager.PublishAsync(fakeRun);

            publisher.Verify(x => x.PublishAsync(It.Is<TestRun>(run => run.TestRunSummary.TotalTests == 7)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun_SummaryDoesnotMatchTestRun()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);
            var fakeRun = new TestRun("fake/1", "somename", 1)
            {
                TestRunSummary = new TestRunSummary
                {
                    TotalPassed = 5,
                    TotalFailed = 3,
                    TotalSkipped = 2
                },
                PassedTests = new List<TestResult>
                {
                    new TestResult()
                },
                FailedTests = new List<TestResult>
                {
                    new TestResult()
                },
                SkippedTests = new List<TestResult>
                {
                    new TestResult()
                }
            };

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>())).Returns(Task.FromResult(GetFakePipelineTestRun(fakeRun, 1)));

            await runManager.PublishAsync(fakeRun);

            publisher.Verify(x => x.PublishAsync(It.Is<TestRun>(run => run.TestRunSummary.TotalTests == 10
                                                                       && run.PassedTests.Count == 0
                                                                       && run.FailedTests.Count == 0
                                                                       && run.SkippedTests.Count == 0
                                                                       && run.TestRunSummary.TotalPassed == 5
                                                                       && run.TestRunSummary.TotalFailed == 3
                                                                       && run.TestRunSummary.TotalSkipped == 2)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishMultipleRuns()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);
            var fakeRun = new TestRun("mocha/1", "somename", 1)
            {
                TestRunSummary = new TestRunSummary
                {
                    TotalTests = 7
                }
            };

            publisher.SetupSequence(x => x.PublishAsync(It.IsAny<TestRun>())).Returns(Task.FromResult(GetFakePipelineTestRun(fakeRun, 1))).Returns(Task.FromResult(GetFakePipelineTestRun(fakeRun, 2)));

            RunTasks(runManager, fakeRun);
            await runManager.FinalizeAsync();

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()), Times.Exactly(2));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishMultipleRunsWithExceptions()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var telemetry = new Mock<ITelemetryDataCollector>();
            var runManager = new TestRunManager(publisher.Object, logger.Object, telemetry.Object);
            var fakeRun = new TestRun("mocha/1", "somename", 1)
            {
                TestRunSummary = new TestRunSummary
                {
                    TotalTests = 7
                }
            };

            publisher.SetupSequence(x => x.PublishAsync(It.IsAny<TestRun>()))
                .Returns(Task.FromResult(GetFakePipelineTestRun(fakeRun, 1)))
                .Returns(Task.FromException<TestRun>(new Exception("some exception ")));

            RunTasks(runManager, fakeRun);
            await runManager.FinalizeAsync();

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()), Times.Exactly(2));
            logger.Verify(x => x.Error(It.IsAny<string>()), Times.Once());
        }

        private TestRun GetFakePipelineTestRun(TestRun testRun, int TcmRunId)
        {
            return new PipelineTestRun(testRun.ParserUri, testRun.RunNamePrefix, testRun.TestRunId, TcmRunId);
        }

        private void RunTasks(ITestRunManager runManager, TestRun testRun)
        {
            runManager.PublishAsync(testRun);
            runManager.PublishAsync(testRun);
        }
    }
}
