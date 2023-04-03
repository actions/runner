using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Results.Client;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(ResultServer))]
    public interface IResultsServer : IRunnerService
    {
        void InitializeResultsClient(Uri uri, string token);

        // logging and console
        Task CreateResultsStepSummaryAsync(string planId, string jobId, Guid stepId, string file,
            CancellationToken cancellationToken);

        Task CreateResultsStepLogAsync(string planId, string jobId, Guid stepId, string file, bool finalize,
            bool firstBlock, long lineCount, CancellationToken cancellationToken);

        Task CreateResultsJobLogAsync(string planId, string jobId, string file, bool finalize, bool firstBlock,
            long lineCount, CancellationToken cancellationToken);

        Task UpdateResultsWorkflowStepsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId,
            IEnumerable<TimelineRecord> records, CancellationToken cancellationToken);
    }

    public sealed class ResultServer : RunnerService, IResultsServer
    {
        private ResultsHttpClient _resultsClient;

        public void InitializeResultsClient(Uri uri, string token)
        {
            var httpMessageHandler = HostContext.CreateHttpClientHandler();
            this._resultsClient = new ResultsHttpClient(uri, httpMessageHandler, token, disposeHandler: true);
        }

        public Task CreateResultsStepSummaryAsync(string planId, string jobId, Guid stepId, string file,
            CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                return _resultsClient.UploadStepSummaryAsync(planId, jobId, stepId, file,
                    cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public Task CreateResultsStepLogAsync(string planId, string jobId, Guid stepId, string file, bool finalize,
            bool firstBlock, long lineCount, CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                return _resultsClient.UploadResultsStepLogAsync(planId, jobId, stepId, file, finalize, firstBlock,
                    lineCount, cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public Task CreateResultsJobLogAsync(string planId, string jobId, string file, bool finalize, bool firstBlock,
            long lineCount, CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                return _resultsClient.UploadResultsJobLogAsync(planId, jobId, file, finalize, firstBlock, lineCount,
                    cancellationToken: cancellationToken);
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }

        public Task UpdateResultsWorkflowStepsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId,
            IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            if (_resultsClient != null)
            {
                try
                {
                    var timelineRecords = records.ToList();
                    return _resultsClient.UpdateWorkflowStepsAsync(planId, new List<TimelineRecord>(timelineRecords),
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log error, but continue as this call is best-effort
                    Trace.Info($"Failed to update steps status due to {ex.GetType().Name}");
                    Trace.Error(ex);
                }
            }

            throw new InvalidOperationException("Results client is not initialized.");
        }
    }
}
