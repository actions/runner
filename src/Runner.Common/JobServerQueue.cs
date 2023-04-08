using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(JobServerQueue))]
    public interface IJobServerQueue : IRunnerService, IThrottlingReporter
    {
        TaskCompletionSource<int> JobRecordUpdated { get; }
        event EventHandler<ThrottlingEventArgs> JobServerQueueThrottling;
        Task ShutdownAsync();
        void Start(Pipelines.AgentJobRequestMessage jobRequest, bool resultServiceOnly = false);
        void QueueWebConsoleLine(Guid stepRecordId, string line, long? lineNumber = null);
        void QueueFileUpload(Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource);
        void QueueResultsUpload(Guid timelineRecordId, string name, string path, string type, bool deleteSource, bool finalize, bool firstBlock, long totalLines);
        void QueueTimelineRecordUpdate(Guid timelineId, TimelineRecord timelineRecord);
    }

    public sealed class JobServerQueue : RunnerService, IJobServerQueue
    {
        // Default delay for Dequeue process
        private static readonly TimeSpan _aggressiveDelayForWebConsoleLineDequeue = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan _delayForWebConsoleLineDequeue = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan _delayForTimelineUpdateDequeue = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan _delayForFileUploadDequeue = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan _delayForResultsUploadDequeue = TimeSpan.FromMilliseconds(1000);

        // Job message information
        private Guid _scopeIdentifier;
        private string _hubName;
        private Guid _planId;
        private Guid _jobTimelineId;
        private Guid _jobTimelineRecordId;

        // queue for web console line
        private readonly ConcurrentQueue<ConsoleLineInfo> _webConsoleLineQueue = new();

        // queue for file upload (log file or attachment)
        private readonly ConcurrentQueue<UploadFileInfo> _fileUploadQueue = new();

        private readonly ConcurrentQueue<ResultsUploadFileInfo> _resultsFileUploadQueue = new();

        // queue for timeline or timeline record update (one queue per timeline)
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<TimelineRecord>> _timelineUpdateQueue = new();

        // indicate how many timelines we have, we will process _timelineUpdateQueue base on the order of timeline in this list
        private readonly List<Guid> _allTimelines = new();

        // bufferd timeline records that fail to update
        private readonly Dictionary<Guid, List<TimelineRecord>> _bufferedRetryRecords = new();

        // Task for each queue's dequeue process
        private Task _webConsoleLineDequeueTask;
        private Task _fileUploadDequeueTask;
        private Task _resultsUploadDequeueTask;
        private Task _timelineUpdateDequeueTask;

        // common
        private IJobServer _jobServer;
        private IResultsServer _resultsServer;
        private ILaunchServer _launchServer;
        private Task[] _allDequeueTasks;
        private readonly TaskCompletionSource<int> _jobCompletionSource = new();
        private readonly TaskCompletionSource<int> _jobRecordUpdated = new();
        private bool _queueInProcess = false;
        private bool _resultsServiceOnly = false;

        public TaskCompletionSource<int> JobRecordUpdated => _jobRecordUpdated;

        public event EventHandler<ThrottlingEventArgs> JobServerQueueThrottling;

        // Web console dequeue will start with process queue every 250ms for the first 60*4 times (~60 seconds).
        // Then the dequeue will happen every 500ms.
        // In this way, customer still can get instance live console output on job start,
        // at the same time we can cut the load to server after the build run for more than 60s
        private int _webConsoleLineAggressiveDequeueCount = 0;
        private const int _webConsoleLineAggressiveDequeueLimit = 4 * 60;
        private const int _webConsoleLineQueueSizeLimit = 1024;
        private bool _webConsoleLineAggressiveDequeue = true;
        private bool _firstConsoleOutputs = true;

        private bool _resultsClientInitiated = false;
        private delegate Task ResultsFileUploadHandler(ResultsUploadFileInfo file);

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _jobServer = hostContext.GetService<IJobServer>();
            _resultsServer = hostContext.GetService<IResultsServer>();
            _launchServer = hostContext.GetService<ILaunchServer>();
        }

        public void Start(Pipelines.AgentJobRequestMessage jobRequest, bool resultServiceOnly = false)
        {
            Trace.Entering();
            _resultsServiceOnly = resultServiceOnly;

            var serviceEndPoint = jobRequest.Resources.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));

            if (!resultServiceOnly)
            {
                _jobServer.InitializeWebsocketClient(serviceEndPoint);
            }

            // This code is usually wrapped by an instance of IExecutionContext which isn't available here.
            if (serviceEndPoint?.Authorization != null &&
                serviceEndPoint.Authorization.Parameters.TryGetValue("AccessToken", out var accessToken) &&
                !string.IsNullOrEmpty(accessToken))
            {
                jobRequest.Variables.TryGetValue("system.github.results_endpoint", out VariableValue resultsEndpointVariable);
                var resultsReceiverEndpoint = resultsEndpointVariable?.Value;
                if (!string.IsNullOrEmpty(resultsReceiverEndpoint))
                {
                    string liveConsoleFeedUrl = null;
                Trace.Info("Initializing results client");
                    if (resultServiceOnly
                    && serviceEndPoint.Data.TryGetValue("FeedStreamUrl", out var feedStreamUrl)
                    && !string.IsNullOrEmpty(feedStreamUrl))
                {
                    liveConsoleFeedUrl = feedStreamUrl;
                }

                _resultsServer.InitializeResultsClient(new Uri(resultsReceiverEndpoint), liveConsoleFeedUrl, accessToken);
                    _resultsClientInitiated = true;
                }

                jobRequest.Variables.TryGetValue("system.github.launch_endpoint", out VariableValue launchEndpointVariable);
                var launchReceiverEndpoint = launchEndpointVariable?.Value;
                if (!string.IsNullOrEmpty(launchReceiverEndpoint))
                {
                    Trace.Info("Initializing launch client");
                    _launchServer.InitializeLaunchClient(new Uri(launchReceiverEndpoint), accessToken);
                }
            }

            if (_queueInProcess)
            {
                Trace.Info("No-opt, all queue process tasks are running.");
                return;
            }

            ArgUtil.NotNull(jobRequest, nameof(jobRequest));
            ArgUtil.NotNull(jobRequest.Plan, nameof(jobRequest.Plan));
            ArgUtil.NotNull(jobRequest.Timeline, nameof(jobRequest.Timeline));

            _scopeIdentifier = jobRequest.Plan.ScopeIdentifier;
            _hubName = jobRequest.Plan.PlanType;
            _planId = jobRequest.Plan.PlanId;
            _jobTimelineId = jobRequest.Timeline.Id;
            _jobTimelineRecordId = jobRequest.JobId;

            // Server already create the job timeline
            _timelineUpdateQueue[_jobTimelineId] = new ConcurrentQueue<TimelineRecord>();
            _allTimelines.Add(_jobTimelineId);

            // Start three dequeue task
            Trace.Info("Start process web console line queue.");
            _webConsoleLineDequeueTask = ProcessWebConsoleLinesQueueAsync();

            Trace.Info("Start process file upload queue.");
            _fileUploadDequeueTask = ProcessFilesUploadQueueAsync();

            Trace.Info("Start results file upload queue.");
            _resultsUploadDequeueTask = ProcessResultsUploadQueueAsync();

            Trace.Info("Start process timeline update queue.");
            _timelineUpdateDequeueTask = ProcessTimelinesUpdateQueueAsync();

            _allDequeueTasks = new Task[] { _webConsoleLineDequeueTask, _fileUploadDequeueTask, _timelineUpdateDequeueTask, _resultsUploadDequeueTask };
            _queueInProcess = true;
        }

        // WebConsoleLine queue and FileUpload queue are always best effort
        // TimelineUpdate queue error will become critical when timeline records contain output variabls.
        public async Task ShutdownAsync()
        {
            if (!_queueInProcess)
            {
                Trace.Info("No-op, all queue process tasks have been stopped.");
            }

            Trace.Info("Fire signal to shutdown all queues.");
            _jobCompletionSource.TrySetResult(0);

            await Task.WhenAll(_allDequeueTasks);
            _queueInProcess = false;
            Trace.Info("All queue process task stopped.");

            // Drain the queue
            // ProcessWebConsoleLinesQueueAsync() will never throw exception, live console update is always best effort.
            Trace.Verbose("Draining web console line queue.");
            await ProcessWebConsoleLinesQueueAsync(runOnce: true);
            Trace.Info("Web console line queue drained.");

            // ProcessFilesUploadQueueAsync() will never throw exception, log file upload is always best effort.
            Trace.Verbose("Draining file upload queue.");
            await ProcessFilesUploadQueueAsync(runOnce: true);
            Trace.Info("File upload queue drained.");

            Trace.Verbose("Draining results upload queue.");
            await ProcessResultsUploadQueueAsync(runOnce: true);
            Trace.Info("Results upload queue drained.");

            // ProcessTimelinesUpdateQueueAsync() will throw exception during shutdown
            // if there is any timeline records that failed to update contains output variabls.
            Trace.Verbose("Draining timeline update queue.");
            await ProcessTimelinesUpdateQueueAsync(runOnce: true);
            Trace.Info("Timeline update queue drained.");

            Trace.Info($"Disposing job server ...");
            await _jobServer.DisposeAsync();

            Trace.Info($"Disposing results server ...");
            await _resultsServer.DisposeAsync();

            Trace.Info("All queue process tasks have been stopped, and all queues are drained.");
        }

        public void QueueWebConsoleLine(Guid stepRecordId, string line, long? lineNumber)
        {
            // We only process 500 lines of the queue everytime.
            // If the queue is backing up due to slow Http request or flood of output from step,
            // we will drop the output to avoid extra memory consumption from the runner since the live console feed is best effort.
            if (!string.IsNullOrEmpty(line) && _webConsoleLineQueue.Count < _webConsoleLineQueueSizeLimit)
            {
                Trace.Verbose("Enqueue web console line queue: {0}", line);
                if (line.Length > 1024)
                {
                    Trace.Verbose("Web console line is more than 1024 chars, truncate to first 1024 chars");
                    line = $"{line.Substring(0, 1024)}...";
                }

                _webConsoleLineQueue.Enqueue(new ConsoleLineInfo(stepRecordId, line, lineNumber));
            }
        }

        public void QueueFileUpload(Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource)
        {
            ArgUtil.NotEmpty(timelineId, nameof(timelineId));
            ArgUtil.NotEmpty(timelineRecordId, nameof(timelineRecordId));

            // all parameter not null, file path exist.
            var newFile = new UploadFileInfo()
            {
                TimelineId = timelineId,
                TimelineRecordId = timelineRecordId,
                Type = type,
                Name = name,
                Path = path,
                DeleteSource = deleteSource
            };

            Trace.Verbose("Enqueue file upload queue: file '{0}' attach to record {1}", newFile.Path, timelineRecordId);
            _fileUploadQueue.Enqueue(newFile);
        }

        public void QueueResultsUpload(Guid timelineRecordId, string name, string path, string type, bool deleteSource, bool finalize, bool firstBlock, long totalLines)
        {
            if (!_resultsClientInitiated)
            {
                Trace.Verbose("Skipping results upload");
                try
                {
                    if (deleteSource)
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    Trace.Info("Catch exception during delete skipped results upload file.");
                    Trace.Error(ex);
                }
                return;
            }

            // all parameter not null, file path exist.
            var newFile = new ResultsUploadFileInfo()
            {
                Name = name,
                Path = path,
                Type = type,
                PlanId = _planId.ToString(),
                JobId = _jobTimelineRecordId.ToString(),
                RecordId = timelineRecordId,
                DeleteSource = deleteSource,
                Finalize = finalize,
                FirstBlock = firstBlock,
                TotalLines = totalLines,
            };

            Trace.Verbose("Enqueue results file upload queue: file '{0}' attach to job {1} step {2}", newFile.Path, _jobTimelineRecordId, timelineRecordId);
            _resultsFileUploadQueue.Enqueue(newFile);
        }

        public void QueueTimelineRecordUpdate(Guid timelineId, TimelineRecord timelineRecord)
        {
            ArgUtil.NotEmpty(timelineId, nameof(timelineId));
            ArgUtil.NotNull(timelineRecord, nameof(timelineRecord));
            ArgUtil.NotEmpty(timelineRecord.Id, nameof(timelineRecord.Id));

            _timelineUpdateQueue.TryAdd(timelineId, new ConcurrentQueue<TimelineRecord>());

            Trace.Verbose("Enqueue timeline {0} update queue: {1}", timelineId, timelineRecord.Id);
            _timelineUpdateQueue[timelineId].Enqueue(timelineRecord.Clone());
        }

        public void ReportThrottling(TimeSpan delay, DateTime expiration)
        {
            Trace.Info($"Receive server throttling report, expect delay {delay} milliseconds till {expiration}");
            var throttlingEvent = JobServerQueueThrottling;
            if (throttlingEvent != null)
            {
                throttlingEvent(this, new ThrottlingEventArgs(delay, expiration));
            }
        }

        private async Task ProcessWebConsoleLinesQueueAsync(bool runOnce = false)
        {
            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                if (_webConsoleLineAggressiveDequeue && ++_webConsoleLineAggressiveDequeueCount > _webConsoleLineAggressiveDequeueLimit)
                {
                    Trace.Info("Stop aggressive process web console line queue.");
                    _webConsoleLineAggressiveDequeue = false;
                }

                // Group consolelines by timeline record of each step
                Dictionary<Guid, List<TimelineRecordLogLine>> stepsConsoleLines = new();
                List<Guid> stepRecordIds = new(); // We need to keep lines in order
                int linesCounter = 0;
                ConsoleLineInfo lineInfo;
                while (_webConsoleLineQueue.TryDequeue(out lineInfo))
                {
                    if (!stepsConsoleLines.ContainsKey(lineInfo.StepRecordId))
                    {
                        stepsConsoleLines[lineInfo.StepRecordId] = new List<TimelineRecordLogLine>();
                        stepRecordIds.Add(lineInfo.StepRecordId);
                    }

                    stepsConsoleLines[lineInfo.StepRecordId].Add(new TimelineRecordLogLine(lineInfo.Line, lineInfo.LineNumber));
                    linesCounter++;

                    // process at most about 500 lines of web console line during regular timer dequeue task.
                    if (!runOnce && linesCounter > 500)
                    {
                        break;
                    }
                }

                // Batch post consolelines for each step timeline record
                foreach (var stepRecordId in stepRecordIds)
                {
                    // Split consolelines into batch, each batch will container at most 100 lines.
                    int batchCounter = 0;
                    List<List<TimelineRecordLogLine>> batchedLines = new();
                    foreach (var line in stepsConsoleLines[stepRecordId])
                    {
                        var currentBatch = batchedLines.ElementAtOrDefault(batchCounter);
                        if (currentBatch == null)
                        {
                            batchedLines.Add(new List<TimelineRecordLogLine>());
                            currentBatch = batchedLines.ElementAt(batchCounter);
                        }

                        currentBatch.Add(line);

                        if (currentBatch.Count >= 100)
                        {
                            batchCounter++;
                        }
                    }

                    if (batchedLines.Count > 0)
                    {
                        // When job finish, web console lines becomes less interesting to customer
                        // We batch and produce 500 lines of web console output every 500ms
                        // If customer's task produce massive of outputs, then the last queue drain run might take forever.
                        // So we will only upload the last 200 lines of each step from all buffered web console lines.
                        if (runOnce && batchedLines.Count > 2)
                        {
                            Trace.Info($"Skip {batchedLines.Count - 2} batches web console lines for last run");
                            batchedLines = batchedLines.TakeLast(2).ToList();
                        }

                        int errorCount = 0;
                        foreach (var batch in batchedLines)
                        {
                            try
                            {
                                // Give at most 60s for each request.
                                using (var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                                {
                                    if (_resultsServiceOnly)
                                    {
                                        await _resultsServer.AppendLiveConsoleFeedAsync(_scopeIdentifier, _hubName, _planId, _jobTimelineId, _jobTimelineRecordId, stepRecordId, batch.Select(logLine => logLine.Line).ToList(), batch[0].LineNumber, timeoutTokenSource.Token);
                                    }
                                    else
                                    {
                                        await _jobServer.AppendTimelineRecordFeedAsync(_scopeIdentifier, _hubName, _planId, _jobTimelineId, _jobTimelineRecordId, stepRecordId, batch.Select(logLine => logLine.Line).ToList(), batch[0].LineNumber, timeoutTokenSource.Token);
                                    }
                                }

                                if (_firstConsoleOutputs)
                                {
                                    HostContext.WritePerfCounter($"WorkerJobServerQueueAppendFirstConsoleOutput_{_planId.ToString()}");
                                    _firstConsoleOutputs = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.Info("Catch exception during append web console line, keep going since the process is best effort.");
                                Trace.Error(ex);
                                errorCount++;
                            }
                        }

                        Trace.Info("Try to append {0} batches web console lines for record '{2}', success rate: {1}/{0}.", batchedLines.Count, batchedLines.Count - errorCount, stepRecordId);
                    }
                }

                if (runOnce)
                {
                    break;
                }
                else
                {
                    await Task.Delay(_webConsoleLineAggressiveDequeue ? _aggressiveDelayForWebConsoleLineDequeue : _delayForWebConsoleLineDequeue);
                }
            }
        }

        private async Task ProcessFilesUploadQueueAsync(bool runOnce = false)
        {
            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                List<UploadFileInfo> filesToUpload = new();
                UploadFileInfo dequeueFile;
                while (_fileUploadQueue.TryDequeue(out dequeueFile))
                {
                    filesToUpload.Add(dequeueFile);
                    // process at most 10 file upload.
                    if (!runOnce && filesToUpload.Count > 10)
                    {
                        break;
                    }
                }

                if (filesToUpload.Count > 0)
                {
                    if (runOnce)
                    {
                        Trace.Info($"Uploading {filesToUpload.Count} files in one shot.");
                    }

                    // TODO: upload all file in parallel
                    int errorCount = 0;
                    foreach (var file in filesToUpload)
                    {
                        try
                        {
                            await UploadFile(file);
                        }
                        catch (Exception ex)
                        {
                            Trace.Info("Catch exception during log or attachment file upload, keep going since the process is best effort.");
                            Trace.Error(ex);
                            errorCount++;

                            // put the failed upload file back to queue.
                            // TODO: figure out how should we retry paging log upload.
                            //lock (_fileUploadQueueLock)
                            //{
                            //    _fileUploadQueue.Enqueue(file);
                            //}
                        }
                    }

                    Trace.Info("Try to upload {0} log files or attachments, success rate: {1}/{0}.", filesToUpload.Count, filesToUpload.Count - errorCount);
                }

                if (runOnce)
                {
                    break;
                }
                else
                {
                    await Task.Delay(_delayForFileUploadDequeue);
                }
            }
        }

        private async Task ProcessResultsUploadQueueAsync(bool runOnce = false)
        {
            Trace.Info("Starting results-based upload queue...");

            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                List<ResultsUploadFileInfo> filesToUpload = new();
                ResultsUploadFileInfo dequeueFile;
                while (_resultsFileUploadQueue.TryDequeue(out dequeueFile))
                {
                    filesToUpload.Add(dequeueFile);
                    // process at most 10 file uploads.
                    if (!runOnce && filesToUpload.Count > 10)
                    {
                        break;
                    }
                }

                if (filesToUpload.Count > 0)
                {
                    if (runOnce)
                    {
                        Trace.Info($"Uploading {filesToUpload.Count} file(s) in one shot through results service.");
                    }

                    int errorCount = 0;
                    foreach (var file in filesToUpload)
                    {
                        try
                        {
                            if (String.Equals(file.Type, ChecksAttachmentType.StepSummary, StringComparison.OrdinalIgnoreCase))
                            {
                                await UploadSummaryFile(file);
                            }
                            else if (String.Equals(file.Type, CoreAttachmentType.ResultsLog, StringComparison.OrdinalIgnoreCase))
                            {
                                if (file.RecordId != _jobTimelineRecordId)
                                {
                                    Trace.Info($"Got a step log file to send to results service.");
                                    await UploadResultsStepLogFile(file);
                                }
                                else if (file.RecordId == _jobTimelineRecordId)
                                {
                                    Trace.Info($"Got a job log file to send to results service.");
                                    await UploadResultsJobLogFile(file);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.Info("Catch exception during file upload to results, keep going since the process is best effort.");
                            Trace.Error(ex);
                            errorCount++;

                            // If we hit any exceptions uploading to Results, let's skip any additional uploads to Results
                            _resultsClientInitiated = false;

                            SendResultsTelemetry(ex);
                        }
                    }

                    Trace.Info("Tried to upload {0} file(s) to results, success rate: {1}/{0}.", filesToUpload.Count, filesToUpload.Count - errorCount);
                }

                if (runOnce)
                {
                    break;
                }
                else
                {
                    await Task.Delay(_delayForResultsUploadDequeue);
                }
            }
        }

        private void SendResultsTelemetry(Exception ex)
        {
            var issue = new Issue() { Type = IssueType.Warning, Message = $"Caught exception with results. {ex.Message}" };
            issue.Data[Constants.Runner.InternalTelemetryIssueDataKey] = Constants.Runner.ResultsUploadFailure;

            var telemetryRecord = new TimelineRecord()
            {
                Id = Constants.Runner.TelemetryRecordId,
            };
            telemetryRecord.Issues.Add(issue);
            QueueTimelineRecordUpdate(_jobTimelineId, telemetryRecord);
        }

        private async Task ProcessTimelinesUpdateQueueAsync(bool runOnce = false)
        {
            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                List<PendingTimelineRecord> pendingUpdates = new();
                foreach (var timeline in _allTimelines)
                {
                    ConcurrentQueue<TimelineRecord> recordQueue;
                    if (_timelineUpdateQueue.TryGetValue(timeline, out recordQueue))
                    {
                        List<TimelineRecord> records = new();
                        TimelineRecord record;
                        while (recordQueue.TryDequeue(out record))
                        {
                            records.Add(record);
                            // process at most 25 timeline records update for each timeline.
                            if (!runOnce && records.Count > 25)
                            {
                                break;
                            }
                        }

                        if (records.Count > 0)
                        {
                            pendingUpdates.Add(new PendingTimelineRecord() { TimelineId = timeline, PendingRecords = records.ToList() });
                        }
                    }
                }

                // we need track whether we have new sub-timeline been created on the last run.
                // if so, we need continue update timeline record even we on the last run.
                bool pendingSubtimelineUpdate = false;
                List<Exception> mainTimelineRecordsUpdateErrors = new();
                if (pendingUpdates.Count > 0)
                {
                    foreach (var update in pendingUpdates)
                    {
                        List<TimelineRecord> bufferedRecords;
                        if (_bufferedRetryRecords.TryGetValue(update.TimelineId, out bufferedRecords))
                        {
                            update.PendingRecords.InsertRange(0, bufferedRecords);
                        }

                        update.PendingRecords = MergeTimelineRecords(update.PendingRecords);

                        foreach (var detailTimeline in update.PendingRecords.Where(r => r.Details != null))
                        {
                            if (!_resultsServiceOnly && !_allTimelines.Contains(detailTimeline.Details.Id))
                            {
                                try
                                {
                                    Timeline newTimeline = await _jobServer.CreateTimelineAsync(_scopeIdentifier, _hubName, _planId, detailTimeline.Details.Id, default(CancellationToken));
                                    _allTimelines.Add(newTimeline.Id);
                                    pendingSubtimelineUpdate = true;
                                }
                                catch (TimelineExistsException)
                                {
                                    Trace.Info("Catch TimelineExistsException during timeline creation. Ignore the error since server already had this timeline.");
                                    _allTimelines.Add(detailTimeline.Details.Id);
                                }
                                catch (Exception ex)
                                {
                                    Trace.Error(ex);
                                }
                            }
                        }

                        try
                        {
                            if (!_resultsServiceOnly)
                            {
                                await _jobServer.UpdateTimelineRecordsAsync(_scopeIdentifier, _hubName, _planId, update.TimelineId, update.PendingRecords, default(CancellationToken));
                            }

                            try
                            {
                                if (_resultsClientInitiated)
                                {
                                    await _resultsServer.UpdateResultsWorkflowStepsAsync(_scopeIdentifier, _hubName, _planId, update.TimelineId, update.PendingRecords, default(CancellationToken));
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.Info("Catch exception during update steps, skip update Results.");
                                Trace.Error(e);
                                _resultsClientInitiated = false;

                                SendResultsTelemetry(e);
                            }

                            if (_bufferedRetryRecords.Remove(update.TimelineId))
                            {
                                Trace.Verbose("Cleanup buffered timeline record for timeline: {0}.", update.TimelineId);
                            }

                            if (!_jobRecordUpdated.Task.IsCompleted &&
                                update.PendingRecords.Any(x => x.Id == _jobTimelineRecordId && x.State != null))
                            {
                                // We have changed the state of the job
                                Trace.Info("Job timeline record has been updated for the first time.");
                                _jobRecordUpdated.TrySetResult(0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.Info("Catch exception during update timeline records, try to update these timeline records next time.");
                            Trace.Error(ex);
                            _bufferedRetryRecords[update.TimelineId] = update.PendingRecords.ToList();
                            if (update.TimelineId == _jobTimelineId)
                            {
                                mainTimelineRecordsUpdateErrors.Add(ex);
                            }
                        }
                    }
                }

                if (runOnce)
                {
                    // continue process timeline records update,
                    // we might have more records need update,
                    // since we just create a new sub-timeline
                    if (pendingSubtimelineUpdate)
                    {
                        continue;
                    }
                    else
                    {
                        if (mainTimelineRecordsUpdateErrors.Count > 0 &&
                            _bufferedRetryRecords.ContainsKey(_jobTimelineId) &&
                            _bufferedRetryRecords[_jobTimelineId] != null &&
                            _bufferedRetryRecords[_jobTimelineId].Any(r => r.Variables.Count > 0))
                        {
                            Trace.Info("Fail to update timeline records with output variables. Throw exception to fail the job since output variables are critical to downstream jobs.");
                            throw new AggregateException("Failed to publish output variables.", mainTimelineRecordsUpdateErrors);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    await Task.Delay(_delayForTimelineUpdateDequeue);
                }
            }
        }

        private List<TimelineRecord> MergeTimelineRecords(List<TimelineRecord> timelineRecords)
        {
            if (timelineRecords == null || timelineRecords.Count <= 1)
            {
                return timelineRecords;
            }

            Dictionary<Guid, TimelineRecord> dict = new();
            foreach (TimelineRecord rec in timelineRecords)
            {
                if (rec == null)
                {
                    continue;
                }

                TimelineRecord timelineRecord;
                if (dict.TryGetValue(rec.Id, out timelineRecord))
                {
                    // Merge rec into timelineRecord
                    timelineRecord.CurrentOperation = rec.CurrentOperation ?? timelineRecord.CurrentOperation;
                    timelineRecord.Details = rec.Details ?? timelineRecord.Details;
                    timelineRecord.FinishTime = rec.FinishTime ?? timelineRecord.FinishTime;
                    timelineRecord.Log = rec.Log ?? timelineRecord.Log;
                    timelineRecord.Name = rec.Name ?? timelineRecord.Name;
                    timelineRecord.RefName = rec.RefName ?? timelineRecord.RefName;
                    timelineRecord.PercentComplete = rec.PercentComplete ?? timelineRecord.PercentComplete;
                    timelineRecord.RecordType = rec.RecordType ?? timelineRecord.RecordType;
                    timelineRecord.Result = rec.Result ?? timelineRecord.Result;
                    timelineRecord.ResultCode = rec.ResultCode ?? timelineRecord.ResultCode;
                    timelineRecord.StartTime = rec.StartTime ?? timelineRecord.StartTime;
                    timelineRecord.State = rec.State ?? timelineRecord.State;
                    timelineRecord.WorkerName = rec.WorkerName ?? timelineRecord.WorkerName;

                    if (rec.ErrorCount != null && rec.ErrorCount > 0)
                    {
                        timelineRecord.ErrorCount = rec.ErrorCount;
                    }

                    if (rec.WarningCount != null && rec.WarningCount > 0)
                    {
                        timelineRecord.WarningCount = rec.WarningCount;
                    }

                    if (rec.NoticeCount != null && rec.NoticeCount > 0)
                    {
                        timelineRecord.NoticeCount = rec.NoticeCount;
                    }

                    if (rec.Issues.Count > 0)
                    {
                        timelineRecord.Issues.Clear();
                        timelineRecord.Issues.AddRange(rec.Issues.Select(i => i.Clone()));
                    }

                    if (rec.Variables.Count > 0)
                    {
                        foreach (var variable in rec.Variables)
                        {
                            timelineRecord.Variables[variable.Key] = variable.Value.Clone();
                        }
                    }
                }
                else
                {
                    dict.Add(rec.Id, rec);
                }
            }

            var mergedRecords = dict.Values.ToList();

            Trace.Verbose("Merged Timeline records");
            foreach (var record in mergedRecords)
            {
                Trace.Verbose($"    Record: t={record.RecordType}, n={record.Name}, s={record.State}, st={record.StartTime}, {record.PercentComplete}%, ft={record.FinishTime}, r={record.Result}: {record.CurrentOperation}");
                if (record.Issues != null && record.Issues.Count > 0)
                {
                    foreach (var issue in record.Issues)
                    {
                        String source;
                        issue.Data.TryGetValue("sourcepath", out source);
                        Trace.Verbose($"        Issue: c={issue.Category}, t={issue.Type}, s={source ?? string.Empty}, m={issue.Message}");
                    }
                }

                if (record.Variables != null && record.Variables.Count > 0)
                {
                    foreach (var variable in record.Variables)
                    {
                        Trace.Verbose($"        Variable: n={variable.Key}, secret={variable.Value.IsSecret}");
                    }
                }
            }

            return mergedRecords;
        }

        private async Task UploadFile(UploadFileInfo file)
        {
            bool uploadSucceed = false;
            try
            {
                if (!_resultsServiceOnly)
                {
                    if (String.Equals(file.Type, CoreAttachmentType.Log, StringComparison.OrdinalIgnoreCase))
                    {
                        // Create the log
                        var taskLog = await _jobServer.CreateLogAsync(_scopeIdentifier, _hubName, _planId, new TaskLog(String.Format(@"logs\{0:D}", file.TimelineRecordId)), default(CancellationToken));

                        // Upload the contents
                        using (FileStream fs = File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var logUploaded = await _jobServer.AppendLogContentAsync(_scopeIdentifier, _hubName, _planId, taskLog.Id, fs, default(CancellationToken));
                        }

                        // Create a new record and only set the Log field
                        var attachmentUpdataRecord = new TimelineRecord() { Id = file.TimelineRecordId, Log = taskLog };
                        QueueTimelineRecordUpdate(file.TimelineId, attachmentUpdataRecord);
                    }
                    else
                    {
                        // Create attachment
                        using (FileStream fs = File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var result = await _jobServer.CreateAttachmentAsync(_scopeIdentifier, _hubName, _planId, file.TimelineId, file.TimelineRecordId, file.Type, file.Name, fs, default(CancellationToken));
                        }
                    }
                }

                uploadSucceed = true;
            }
            finally
            {
                if (uploadSucceed && file.DeleteSource)
                {
                    try
                    {
                        File.Delete(file.Path);
                    }
                    catch (Exception ex)
                    {
                        Trace.Info("Catch exception during delete success uploaded file.");
                        Trace.Error(ex);
                    }
                }
            }
        }

        private async Task UploadSummaryFile(ResultsUploadFileInfo file)
        {
            Trace.Info($"Starting to upload summary file to results service {file.Name}, {file.Path}");
            ResultsFileUploadHandler summaryHandler = async (file) =>
            {
                await _resultsServer.CreateResultsStepSummaryAsync(file.PlanId, file.JobId, file.RecordId, file.Path, CancellationToken.None);
            };

            await UploadResultsFile(file, summaryHandler);
        }

        private async Task UploadResultsStepLogFile(ResultsUploadFileInfo file)
        {
            Trace.Info($"Starting upload of step log file to results service {file.Name}, {file.Path}");
            ResultsFileUploadHandler stepLogHandler = async (file) =>
            {
                await _resultsServer.CreateResultsStepLogAsync(file.PlanId, file.JobId, file.RecordId, file.Path, file.Finalize, file.FirstBlock, file.TotalLines, CancellationToken.None);
            };

            await UploadResultsFile(file, stepLogHandler);
        }

        private async Task UploadResultsJobLogFile(ResultsUploadFileInfo file)
        {
            Trace.Info($"Starting upload of job log file to results service {file.Name}, {file.Path}");
            ResultsFileUploadHandler jobLogHandler = async (file) =>
            {
                await _resultsServer.CreateResultsJobLogAsync(file.PlanId, file.JobId, file.Path, file.Finalize, file.FirstBlock, file.TotalLines, CancellationToken.None);
            };

            await UploadResultsFile(file, jobLogHandler);
        }

        private async Task UploadResultsFile(ResultsUploadFileInfo file, ResultsFileUploadHandler uploadHandler)
        {
            if (!_resultsClientInitiated)
            {
                return;
            }

            bool uploadSucceed = false;
            try
            {
                await uploadHandler(file);
                uploadSucceed = true;
            }
            finally
            {
                if (uploadSucceed && file.DeleteSource)
                {
                    try
                    {
                        File.Delete(file.Path);
                    }
                    catch (Exception ex)
                    {
                        Trace.Info("Exception encountered during deletion of a temporary file that was already successfully uploaded to results.");
                        Trace.Error(ex);
                    }
                }
            }
        }
    }

    internal class PendingTimelineRecord
    {
        public Guid TimelineId { get; set; }
        public List<TimelineRecord> PendingRecords { get; set; }
    }

    internal class UploadFileInfo
    {
        public Guid TimelineId { get; set; }
        public Guid TimelineRecordId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool DeleteSource { get; set; }
    }

    internal class ResultsUploadFileInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
        public string PlanId { get; set; }
        public string JobId { get; set; }
        public Guid RecordId { get; set; }
        public bool DeleteSource { get; set; }
        public bool Finalize { get; set; }
        public bool FirstBlock { get; set; }
        public long TotalLines { get; set; }
    }

    internal class ConsoleLineInfo
    {
        public ConsoleLineInfo(Guid recordId, string line, long? lineNumber)
        {
            this.StepRecordId = recordId;
            this.Line = line;
            this.LineNumber = lineNumber;
        }

        public Guid StepRecordId { get; set; }
        public string Line { get; set; }
        public long? LineNumber { get; set; }
    }
}
