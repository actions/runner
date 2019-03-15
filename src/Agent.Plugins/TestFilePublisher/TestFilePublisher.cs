using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Microsoft.TeamFoundation.TestClient.PublishTestResults;
using Microsoft.VisualStudio.Services.WebApi;
using ITestResultParser = Microsoft.TeamFoundation.TestClient.PublishTestResults.ITestResultParser;
using ITestRunPublisher = Microsoft.TeamFoundation.TestClient.PublishTestResults.ITestRunPublisher;

namespace Agent.Plugins.Log.TestFilePublisher
{
    public interface ITestFilePublisher
    {
        Task InitializeAsync();
        Task PublishAsync();
    }

    public class TestFilePublisher : ITestFilePublisher
    {
        private readonly VssConnection _vssConnection;
        private readonly PipelineConfig _pipelineConfig;
        private readonly TraceListener _traceListener;
        private readonly ITraceLogger _logger;
        private readonly ITelemetryDataCollector _telemetry;
        private ITestFileFinder _testFileFinder;
        private ITestResultParser _testResultParser;
        private ITestRunPublisher _testRunPublisher;

        public TestFilePublisher(VssConnection vssConnection, PipelineConfig pipelineConfig, TraceListener traceListener,
            ITraceLogger logger, ITelemetryDataCollector telemetry)
        {
            _traceListener = traceListener;
            _vssConnection = vssConnection;
            _pipelineConfig = pipelineConfig;
            _logger = logger;
            _telemetry = telemetry;
        }

        public TestFilePublisher(VssConnection vssConnection, PipelineConfig pipelineConfig, TraceListener traceListener,
            ITraceLogger logger, ITelemetryDataCollector telemetry, ITestFileFinder testFileFinder,
            ITestResultParser testResultParser, ITestRunPublisher testRunPublisher)
        : this(vssConnection, pipelineConfig, traceListener, logger, telemetry)
        {
            _testFileFinder = testFileFinder;
            _testResultParser = testResultParser;
            _testRunPublisher = testRunPublisher;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() => Initialize());
        }

        public async Task PublishAsync()
        {
            var testResultFiles = new List<string>();
            IList<TestRunData> testData;

            var testRunContext = new TestRunContextBuilder("JUnit test results")
                .WithBuildId(_pipelineConfig.BuildId)
                .WithBuildUri(_pipelineConfig.BuildUri)
                .Build();

            using (new SimpleTimer(TelemetryConstants.FindTestFilesAsync, _logger, TimeSpan.FromSeconds(60),
                new TelemetryDataWrapper(_telemetry, TelemetryConstants.FindTestFilesAsync)))
            {
                try
                {
                    testResultFiles.AddRange(await FindTestFilesAsync());

                    _logger.Info($"Number of files found with matching pattern {testResultFiles.Count}");
                }
                catch (Exception ex)
                {
                    _logger.Info($"Error: {ex.Message}");
                    _telemetry.AddOrUpdate("FindTestFilesError", ex);
                }
            }

            _telemetry.AddOrUpdate("NumberOfTestFilesFound", testResultFiles.Count);
            if (!testResultFiles.Any())
            {
                _logger.Info("No test result files are found");
                return;
            }

            using (new SimpleTimer(TelemetryConstants.ParseTestResultFiles, _logger, TimeSpan.FromSeconds(60),
                new TelemetryDataWrapper(_telemetry, TelemetryConstants.ParseTestResultFiles)))
            {
                testData = _testResultParser.ParseTestResultFiles(testRunContext, testResultFiles).GetTestRunData();

                _logger.Info($"Successfully parsed {testData?.Count} files");
                _telemetry.AddOrUpdate("NumberOfTestFilesRead", testData?.Count);
            }

            if (testData == null || !testData.Any())
            {
                _logger.Info("No valid Junit test result files are found which can be parsed");
                return;
            }

            using (new SimpleTimer(TelemetryConstants.PublishTestRunDataAsync, _logger, TimeSpan.FromSeconds(60),
                new TelemetryDataWrapper(_telemetry, TelemetryConstants.PublishTestRunDataAsync)))
            {
                var publishedRuns = await _testRunPublisher.PublishTestRunDataAsync(testRunContext, _pipelineConfig.ProjectName, testData, new PublishOptions(),
                    new CancellationToken());

                if (publishedRuns != null)
                {
                    _logger.Info($"Successfully published {publishedRuns.Count} runs");
                    _telemetry.AddOrUpdate("NumberOfTestRunsPublished", publishedRuns.Count);
                    _telemetry.AddOrUpdate("TestRunIds", string.Join(",", publishedRuns.Select(x => x.Id)));
                }
                else
                {
                    _telemetry.AddOrUpdate("NumberOfTestRunsPublished", 0);
                }
            }
        }

        protected async Task<IEnumerable<string>> FindTestFilesAsync()
        {
            return await _testFileFinder.FindAsync(_pipelineConfig.Patterns);
        }

        private void Initialize()
        {
            _testFileFinder = _testFileFinder ?? new TestFileFinder(_pipelineConfig.SearchFolders);
            _testResultParser = _testResultParser ?? new JUnitResultParser(_traceListener);
            _testRunPublisher = _testRunPublisher ?? new TestRunPublisher(_vssConnection, _traceListener);
        }
    }
}
