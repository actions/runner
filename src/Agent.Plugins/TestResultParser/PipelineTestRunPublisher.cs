// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.TestResults.WebApi;
using TestOutcome = Microsoft.TeamFoundation.TestManagement.WebApi.TestOutcome;
using TestRun = Agent.Plugins.Log.TestResultParser.Contracts.TestRun;

namespace Agent.Plugins.TestResultParser.Plugin
{
    public class PipelineTestRunPublisher : ITestRunPublisher
    {
        public PipelineTestRunPublisher(IClientFactory clientFactory, IPipelineConfig pipelineConfig)
        {
            _pipelineConfig = pipelineConfig;
            _httpClient = clientFactory.GetClient<TestResultsHttpClient>();
        }

        /// <inheritdoc />
        public async Task PublishAsync(TestRun testRun)
        {
            var runUri = testRun.ParserUri.Split("/");
            var r = new RunCreateModel(name: $"{runUri[0]} test run {testRun.TestRunId} - automatically inferred results", buildId: _pipelineConfig.BuildId,
                state: TestRunState.InProgress.ToString(), isAutomated: true, type: RunType.NoConfigRun.ToString());
            var run = await _httpClient.CreateTestRunAsync(r, _pipelineConfig.Project);

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

            await _httpClient.AddTestResultsToTestRunAsync(testResults.ToArray(), _pipelineConfig.Project, run.Id);

            //  var runUpdateModel = new RunUpdateModel(state: TestRunState.Completed.ToString());

            var runUpdateModel = new RunUpdateModel(state: TestRunState.Completed.ToString())
            {
                RunSummary = new List<RunSummaryModel>()
            };

            runUpdateModel.RunSummary.Add(new RunSummaryModel(resultCount: testRun.TestRunSummary.TotalFailed, testOutcome: TestOutcome.Failed));
            runUpdateModel.RunSummary.Add(new RunSummaryModel(resultCount: testRun.TestRunSummary.TotalPassed, testOutcome: TestOutcome.Passed));
            runUpdateModel.RunSummary.Add(new RunSummaryModel(resultCount: testRun.TestRunSummary.TotalSkipped, testOutcome: TestOutcome.NotExecuted));


            await _httpClient.UpdateTestRunAsync(runUpdateModel, _pipelineConfig.Project, run.Id);
        }

        private readonly TestResultsHttpClient _httpClient;
        private readonly IPipelineConfig _pipelineConfig;
    }
}
