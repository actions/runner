// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    /// <inheritdoc/>
    public class TestRunManager : ITestRunManager
    {
        private readonly ITestRunPublisher _publisher;
        private readonly ITraceLogger _logger;
        private readonly ITelemetryDataCollector _telemetry;
        private readonly List<Task> _runningTasks = new List<Task>();

        /// <summary>
        /// Construct the TestRunManger
        /// </summary>
        public TestRunManager(ITestRunPublisher testRunPublisher, ITraceLogger logger, ITelemetryDataCollector telemetry)
        {
            _publisher = testRunPublisher;
            _logger = logger;
            _telemetry = telemetry;
        }

        /// <summary>
        /// Publish test run to pipeline
        /// </summary>
        public async Task PublishAsync(TestRun testRun)
        {
            _telemetry.AddAndAggregate(TelemetryConstants.TotalRunsDetected, 1);

            var validatedTestRun = ValidateAndPrepareForPublish(testRun);
            if (validatedTestRun != null)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.ValidRunsDetected, 1);
                var task = _publisher.PublishAsync(validatedTestRun);
                _runningTasks.Add(task);
                await task;
            }
        }

        /// <summary>
        /// Complete pending test runs
        /// </summary>
        public async Task FinalizeAsync()
        {
            try
            {
                await Task.WhenAll(_runningTasks.ToArray());
            }
            catch (Exception ex)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.Exceptions, new List<Exception> { ex },
                    TelemetryConstants.TestRunManagerEventArea);
                _logger.Error($"TestRunManager : FinalizeAsync : Failed to complete test run. Error: {ex}");
            }
        }

        private TestRun ValidateAndPrepareForPublish(TestRun testRun)
        {
            if (testRun?.TestRunSummary == null)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.RunSummaryNull, 1, TelemetryConstants.TestRunManagerEventArea);
                _logger.Error("TestRunManger : ValidateAndPrepareForPublish : TestRun or TestRunSummary is null.");
                return null;
            }

            // TotalTests count should always be less than passed and failed test count combined
            if (testRun.TestRunSummary.TotalTests < testRun.TestRunSummary.TotalFailed + testRun.TestRunSummary.TotalPassed + testRun.TestRunSummary.TotalSkipped)
            {
                testRun.TestRunSummary.TotalTests = testRun.TestRunSummary.TotalFailed + testRun.TestRunSummary.TotalPassed + testRun.TestRunSummary.TotalSkipped;
            }

            if (testRun.TestRunSummary.TotalTests == 0)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.TotalTestsZero, 1, TelemetryConstants.TestRunManagerEventArea);
                _logger.Error("TestRunManger : ValidateAndPrepareForPublish : No tests found.");
                return null;
            }

            // Match the passed test count and clear the passed tests collection if mismatch occurs
            if (testRun.TestRunSummary.TotalPassed != testRun.PassedTests?.Count)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.PassedCountMismatch, 1, TelemetryConstants.TestRunManagerEventArea);
                _logger.Warning("TestRunManger : ValidateAndPrepareForPublish : Passed test count does not match the Test summary.");

                testRun.PassedTests = new List<TestResult>();
            }

            // Match the failed test count and clear the failed tests collection if mismatch occurs
            if (testRun.TestRunSummary.TotalFailed != testRun.FailedTests?.Count)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.FailedCountMismatch, 1, TelemetryConstants.TestRunManagerEventArea);
                _logger.Warning("TestRunManger : ValidateAndPrepareForPublish : Failed test count does not match the Test summary.");
                testRun.FailedTests = new List<TestResult>();
            }

            // Match the skipped test count and clear the failed tests collection if mismatch occurs
            if (testRun.TestRunSummary.TotalSkipped != testRun.SkippedTests?.Count)
            {
                _telemetry.AddAndAggregate(TelemetryConstants.SkippedCountMismatch, 1, TelemetryConstants.TestRunManagerEventArea);
                _logger.Warning("TestRunManger : ValidateAndPrepareForPublish : Skipped test count does not match the Test summary.");
                testRun.SkippedTests = new List<TestResult>();
            }

            return testRun;
        }
    }
}
