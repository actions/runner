using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestFilePublisher;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Microsoft.TeamFoundation.TestClient.PublishTestResults;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Xunit;
using ITestResultParser = Microsoft.TeamFoundation.TestClient.PublishTestResults.ITestResultParser;
using ITestRunPublisher = Microsoft.TeamFoundation.TestClient.PublishTestResults.ITestRunPublisher;
using TestRun = Microsoft.TeamFoundation.TestManagement.WebApi.TestRun;

namespace Test.L0.Plugin.TestFilePublisher
{
    public class TestFilePublisherL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisher_PublishTestFiles()
        {
            var publisher = new Agent.Plugins.Log.TestFilePublisher.TestFilePublisher(_vssConnection.Object, _pipelineConfig, _traceListener.Object, _logger.Object,
                _telemetry.Object, _testFileFinder.Object, _testResultParser.Object, _testRunPublisher.Object);

            var testFiles = new List<string>
            {
                "/tmp/test-1.xml"
            };
            var testRuns = new List<TestRun>
            {
                new TestRun()
            };

            _testFileFinder.Setup(x => x.FindAsync(It.IsAny<IList<string>>())).ReturnsAsync(testFiles.AsEnumerable());
            _testResultParser.Setup(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()))
                .Returns(new TestDataProvider(new List<TestData>
                {
                    new TestData()
                    {
                        TestRunData = new TestRunData(null)
                    }
                }));
            _testRunPublisher.Setup(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(testRuns);

            await publisher.InitializeAsync();
            await publisher.PublishAsync();

            _testFileFinder.Verify(x => x.FindAsync(It.IsAny<IList<string>>()), Times.Once);
            _testResultParser.Verify(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()), Times.Once);
            _testRunPublisher.Verify(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Once);

            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Number of files found with matching pattern 1"))), Times.Once);
            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Successfully parsed 1 files"))), Times.Once);
            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Successfully published 1 runs"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisher_PublishMultipleFiles()
        {
            var publisher = new Agent.Plugins.Log.TestFilePublisher.TestFilePublisher(_vssConnection.Object, _pipelineConfig, _traceListener.Object, _logger.Object,
                _telemetry.Object, _testFileFinder.Object, _testResultParser.Object, _testRunPublisher.Object);

            var testFiles = new List<string>
            {
                "/tmp/test-1.xml",
                "/tmp/test-2.xml",
                "/tmp/test-3.xml",
            };
            var testRuns = new List<TestRun>
            {
                new TestRun()
            };

            _testFileFinder.Setup(x => x.FindAsync(It.IsAny<IList<string>>())).ReturnsAsync(testFiles.AsEnumerable());
            _testResultParser.Setup(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()))
                .Returns(new TestDataProvider(new List<TestData>
                {
                    new TestData
                    {
                        TestRunData = new TestRunData(null)
                    }, new TestData()
                    {
                        TestRunData = new TestRunData(null)
                    }
                }));
            _testRunPublisher.Setup(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(testRuns);

            await publisher.InitializeAsync();
            await publisher.PublishAsync();

            _testFileFinder.Verify(x => x.FindAsync(It.IsAny<IList<string>>()), Times.Once);
            _testResultParser.Verify(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()), Times.Once);
            _testRunPublisher.Verify(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Once);


            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Number of files found with matching pattern 3"))), Times.Once);
            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Successfully parsed 2 files"))), Times.Once);
            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Successfully published 1 runs"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisher_DontPublishWhenNoMatchingFilesFound()
        {
            var publisher = new Agent.Plugins.Log.TestFilePublisher.TestFilePublisher(_vssConnection.Object, _pipelineConfig, _traceListener.Object, _logger.Object,
                _telemetry.Object, _testFileFinder.Object, _testResultParser.Object, _testRunPublisher.Object);

            _testFileFinder.Setup(x => x.FindAsync(It.IsAny<IList<string>>())).ReturnsAsync(Enumerable.Empty<string>());
            _testResultParser.Setup(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()))
                .Throws<Exception>();
            _testRunPublisher.Setup(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>())).Throws<Exception>();

            await publisher.InitializeAsync();
            await publisher.PublishAsync();

            _testFileFinder.Verify(x => x.FindAsync(It.IsAny<IList<string>>()), Times.Once);

            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("No test result files are found"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisher_DontPublishWhenFileExceptionsAreThrown()
        {
            var publisher = new Agent.Plugins.Log.TestFilePublisher.TestFilePublisher(_vssConnection.Object, _pipelineConfig, _traceListener.Object, _logger.Object,
                _telemetry.Object, _testFileFinder.Object, _testResultParser.Object, _testRunPublisher.Object);

            _testFileFinder.Setup(x => x.FindAsync(It.IsAny<IList<string>>())).Throws<Exception>();
            _testResultParser.Setup(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()))
                .Throws<Exception>();
            _testRunPublisher.Setup(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>())).Throws<Exception>();

            await publisher.InitializeAsync();
            await publisher.PublishAsync();

            _testFileFinder.Verify(x => x.FindAsync(It.IsAny<IList<string>>()), Times.Once);

            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("No test result files are found"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisher_DontPublishWhenFilesAreNotValid()
        {
            var publisher = new Agent.Plugins.Log.TestFilePublisher.TestFilePublisher(_vssConnection.Object, _pipelineConfig, _traceListener.Object, _logger.Object,
                _telemetry.Object, _testFileFinder.Object, _testResultParser.Object, _testRunPublisher.Object);

            var testFiles = new List<string>
            {
                "/tmp/test-1.xml",
                "/tmp/test-2.xml",
                "/tmp/test-3.xml",
            };

            _testFileFinder.Setup(x => x.FindAsync(It.IsAny<IList<string>>())).ReturnsAsync(testFiles.AsEnumerable());
            _testResultParser.Setup(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()))
                .Returns(new TestDataProvider(null));

            _testRunPublisher.Setup(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>())).Throws<Exception>();

            await publisher.InitializeAsync();
            await publisher.PublishAsync();

            _testFileFinder.Verify(x => x.FindAsync(It.IsAny<IList<string>>()), Times.Once);

            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("No valid Junit test result files are found which can be parsed"))), Times.Once);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFilePublisher_WhenPublishedRunsAreNull()
        {
            var publisher = new Agent.Plugins.Log.TestFilePublisher.TestFilePublisher(_vssConnection.Object, _pipelineConfig, _traceListener.Object, _logger.Object,
                _telemetry.Object, _testFileFinder.Object, _testResultParser.Object, _testRunPublisher.Object);

            var testFiles = new List<string>
            {
                "/tmp/test-1.xml"
            };
            List<TestRun> testRuns = null;

            _testFileFinder.Setup(x => x.FindAsync(It.IsAny<IList<string>>())).ReturnsAsync(testFiles.AsEnumerable());
            _testResultParser.Setup(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()))
                .Returns(new TestDataProvider(new List<TestData>
                {
                    new TestData()
                    {
                        TestRunData = new TestRunData(null)
                    }
                }));
            _testRunPublisher.Setup(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(testRuns);

            await publisher.InitializeAsync();
            await publisher.PublishAsync();

            _testFileFinder.Verify(x => x.FindAsync(It.IsAny<IList<string>>()), Times.Once);
            _testResultParser.Verify(x => x.ParseTestResultFiles(It.IsAny<TestRunContext>(), It.IsAny<IList<string>>()), Times.Once);
            _testRunPublisher.Verify(x => x.PublishTestRunDataAsync(It.IsAny<TestRunContext>(), It.IsAny<string>(), It.IsAny<IList<TestRunData>>(),
                It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Once);

            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Number of files found with matching pattern 1"))), Times.Once);
            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Successfully parsed 1 files"))), Times.Once);
            _logger.Verify(x => x.Info(It.Is<string>(msg => msg.Contains("Successfully published"))), Times.Never);
        }

        private readonly Mock<ITraceLogger> _logger = new Mock<ITraceLogger>();
        private readonly Mock<ITelemetryDataCollector> _telemetry = new Mock<ITelemetryDataCollector>();
        private readonly Mock<VssConnection> _vssConnection = new Mock<VssConnection>(new Uri("http://fake"), new VssCredentials());
        private readonly PipelineConfig _pipelineConfig = new PipelineConfig();
        private readonly Mock<TraceListener> _traceListener = new Mock<TraceListener>();
        private readonly Mock<ITestFileFinder> _testFileFinder = new Mock<ITestFileFinder>();
        private readonly Mock<ITestResultParser> _testResultParser = new Mock<ITestResultParser>();
        private readonly Mock<ITestRunPublisher> _testRunPublisher = new Mock<ITestRunPublisher>();
    }
}
