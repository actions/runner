using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.TestResults.WebApi;
using TestOutcome = Microsoft.TeamFoundation.TestManagement.WebApi.TestOutcome;
using TestRun = Agent.Plugins.Log.TestResultParser.Contracts.TestRun;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class PipelineTestRunPublisher : ITestRunPublisher
    {
        public PipelineTestRunPublisher(IClientFactory clientFactory, IPipelineConfig pipelineConfig, ITraceLogger logger, ITelemetryDataCollector telemetry)
        {
            _logger = logger;
            _telemetry = telemetry;
            _pipelineConfig = pipelineConfig;
            _httpClient = clientFactory.GetClient<TestResultsHttpClient>();
        }

        /// <inheritdoc />
        public async Task<TestRun> PublishAsync(TestRun testRun)
        {
            using (var timer = new SimpleTimer("PublishTestRun", _logger,
                new TelemetryDataWrapper(_telemetry, TelemetryConstants.PipelineTestRunPublisherEventArea, TelemetryConstants.PublishTestRun),
                TimeSpan.FromMilliseconds(int.MaxValue)))
            {
                var runCreateModel = new RunCreateModel(name: testRun.TestRunName, buildId: _pipelineConfig.BuildId,
                    state: TestRunState.InProgress.ToString(), isAutomated: true, type: RunType.NoConfigRun.ToString());

                // Create the test run on the server
                var run = await _httpClient.CreateTestRunAsync(runCreateModel, _pipelineConfig.Project);
                _logger.Info($"PipelineTestRunPublisher : PublishAsync : Created test run with id {run.Id}.");
                _telemetry.AddAndAggregate(TelemetryConstants.TestRunIds, new List<int> { run.Id },
                    TelemetryConstants.PipelineTestRunPublisherEventArea);
                _telemetry.AddAndAggregate($"{testRun.ParserUri.Split('/')[0]}RunsCount", 1,
                    TelemetryConstants.PipelineTestRunPublisherEventArea);

                // Populate test reulsts
                var testResults = new List<TestCaseResult>();

                foreach (var passedTest in testRun.PassedTests)
                {
                    testResults.Add(new TestCaseResult
                    {
                        TestCaseTitle = passedTest.Name,
                        AutomatedTestName = passedTest.Name,
                        DurationInMs = passedTest.ExecutionTime.TotalMilliseconds,
                        State = "Completed",
                        AutomatedTestType = "NoConfig",
                        Outcome = TestOutcome.Passed.ToString()
                    });
                }

                foreach (var failedTest in testRun.FailedTests)
                {
                    testResults.Add(new TestCaseResult
                    {
                        TestCaseTitle = failedTest.Name,
                        AutomatedTestName = failedTest.Name,
                        DurationInMs = failedTest.ExecutionTime.TotalMilliseconds,
                        State = "Completed",
                        AutomatedTestType = "NoConfig",
                        Outcome = TestOutcome.Failed.ToString(),
                        StackTrace = failedTest.StackTrace
                    });
                }

                foreach (var skippedTest in testRun.SkippedTests)
                {
                    testResults.Add(new TestCaseResult
                    {
                        TestCaseTitle = skippedTest.Name,
                        AutomatedTestName = skippedTest.Name,
                        DurationInMs = skippedTest.ExecutionTime.TotalMilliseconds,
                        State = "Completed",
                        AutomatedTestType = "NoConfig",
                        Outcome = TestOutcome.NotExecuted.ToString()

                    });
                }

                // Update the run with test results
                await _httpClient.AddTestResultsToTestRunAsync(testResults.ToArray(), _pipelineConfig.Project, run.Id);

                var runUpdateModel = new RunUpdateModel(state: TestRunState.Completed.ToString())
                {
                    RunSummary = new List<RunSummaryModel>()
                };

                runUpdateModel.RunSummary.Add(new RunSummaryModel(resultCount: testRun.TestRunSummary.TotalFailed, testOutcome: TestOutcome.Failed));
                runUpdateModel.RunSummary.Add(new RunSummaryModel(resultCount: testRun.TestRunSummary.TotalPassed, testOutcome: TestOutcome.Passed));
                runUpdateModel.RunSummary.Add(new RunSummaryModel(resultCount: testRun.TestRunSummary.TotalSkipped, testOutcome: TestOutcome.NotExecuted));

                // Complete the run
                await _httpClient.UpdateTestRunAsync(runUpdateModel, _pipelineConfig.Project, run.Id);

                return new PipelineTestRun(testRun.ParserUri, testRun.RunNamePrefix, testRun.TestRunId, run.Id);
            }
        }

        private readonly TestResultsHttpClient _httpClient;
        private readonly IPipelineConfig _pipelineConfig;
        private readonly ITraceLogger _logger;
        private readonly ITelemetryDataCollector _telemetry;
    }
}
