using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(JobServerQueue))]
    public interface IJobServerQueue : IAgentService
    {
        Task ShutdownAsync();
        void Start(JobRequestMessage jobRequest);
        void QueueWebConsoleLine(string line);
        void QueueFileUpload(Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource);
        void QueueTimelineRecordUpdate(Guid timelineId, TimelineRecord timelineRecord);
    }

    public sealed class JobServerQueue : AgentService, IJobServerQueue
    {
        // Default delay for Dequeue process
        private static readonly TimeSpan _delayForWebConsoleLineDequeue = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan _delayForTimelineUpdateDequeue = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan _delayForFileUploadDequeue = TimeSpan.FromMilliseconds(1000);

        // Threshold that enqueue method will signal a dequeue run immediately
        private const int _webConsoleLineQueueForceProcessThreshold = 100;
        private const int _timelineUpdateQueueForceProcessThreshold = 25;
        private const int _fileUploadQueueForceProcessThreshold = 5;

        // Job message information
        private Guid _scopeIdentifier;
        private string _hubName;
        private Guid _planId;
        private Guid _jobTimelineId;
        private Guid _jobTimelineRecordId;

        // queue for web console line
        private readonly Queue<string> _webConsoleLineQueue = new Queue<string>();

        // queue for file upload (log file or attachment)
        private readonly Queue<UploadFileInfo> _fileUploadQueue = new Queue<UploadFileInfo>();

        // queue for timeline or timeline record update (one queue per timeline)
        private readonly Dictionary<Guid, Queue<TimelineRecord>> _timelineUpdateQueue = new Dictionary<Guid, Queue<TimelineRecord>>();

        // indicate how many timelines we have, we will process _timelineUpdateQueue base on the order of timeline in this list
        private readonly List<Guid> _allTimelines = new List<Guid>();

        // bufferd timeline records that fail to update
        private readonly Dictionary<Guid, List<TimelineRecord>> _bufferedRetryRecords = new Dictionary<Guid, List<TimelineRecord>>();

        // lock for each queue
        private readonly object _webConsoleLineQueueLock = new object();
        private readonly object _fileUploadQueueLock = new object();
        private readonly object _timelineUpdateQueueLock = new object();

        // Task for each queue's dequeue process
        private Task _webConsoleLineDequeueTask;
        private Task _fileUploadDequeueTask;
        private Task _timelineUpdateDequeueTask;

        // Semaphore for each queue
        private readonly SemaphoreSlim _webConsoleLineQueueSemaphore = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _fileUploadQueueSemaphore = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _timelineUpdateQueueSemaphore = new SemaphoreSlim(0, 1);

        // common
        private IJobServer _jobServer;
        private Task[] _allDequeueTasks;
        private readonly TaskCompletionSource<int> _jobCompletionSource = new TaskCompletionSource<int>();
        private bool _queueInProcess = false;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _jobServer = hostContext.GetService<IJobServer>();
        }

        public void Start(JobRequestMessage jobRequest)
        {
            Trace.Entering();
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
            _timelineUpdateQueue[_jobTimelineId] = new Queue<TimelineRecord>();
            _allTimelines.Add(_jobTimelineId);

            // Start three dequeue task
            Trace.Info("Start process web console line queue.");
            _webConsoleLineDequeueTask = ProcessWebConsoleLinesQueueAsync();

            Trace.Info("Start process file upload queue.");
            _fileUploadDequeueTask = ProcessFilesUploadQueueAsync();

            Trace.Info("Start process timeline update queue.");
            _timelineUpdateDequeueTask = ProcessTimelinesUpdateQueueAsync();

            _allDequeueTasks = new Task[] { _webConsoleLineDequeueTask, _fileUploadDequeueTask, _timelineUpdateDequeueTask };
            _queueInProcess = true;
        }

        public async Task ShutdownAsync()
        {
            if (!_queueInProcess)
            {
                Trace.Info("No-opt, all queue process tasks have been stopped.");
            }

            Trace.Verbose("Fire signal to shutdown all queues.");
            _jobCompletionSource.TrySetResult(0);

            await Task.WhenAll(_allDequeueTasks);
            _queueInProcess = false;
            Trace.Info("All queue process task stopped.");
            

            //Drain the queue
            List<Exception> queueShutdownExceptions = new List<Exception>();
            try
            {
                Trace.Verbose("Draining web console line queue.");
                await ProcessWebConsoleLinesQueueAsync(runOnce: true);
                Trace.Info("Web console line queue drained.");
            }
            catch (Exception ex)
            {
                Trace.Error("Drain web console line queue fail with: {0}", ex.Message);
                queueShutdownExceptions.Add(ex);
            }

            try
            {
                Trace.Verbose("Draining file upload queue.");
                await ProcessFilesUploadQueueAsync(runOnce: true);
                Trace.Info("File upload queue drained.");
            }
            catch (Exception ex)
            {
                Trace.Error("Drain file upload queue fail with: {0}", ex.Message);
                queueShutdownExceptions.Add(ex);
            }

            try
            {
                Trace.Verbose("Draining timeline update queue.");
                await ProcessTimelinesUpdateQueueAsync(runOnce: true);
                Trace.Info("Timeline update queue drained.");
            }
            catch (Exception ex)
            {
                Trace.Error("Drain timeline update queue fail with: {0}", ex.Message);
                queueShutdownExceptions.Add(ex);
            }

            if (queueShutdownExceptions.Count > 0)
            {
                throw new AggregateException("Catch exceptions during queue shutdown.", queueShutdownExceptions);
            }
            else
            {
                Trace.Info("All queue process tasks have been stopped, and all queues are drained.");
            }
        }

        public void QueueWebConsoleLine(string line)
        {
            lock (_webConsoleLineQueueLock)
            {
                Trace.Verbose("Enqueue web console line queue: {0}", line);
                _webConsoleLineQueue.Enqueue(line);
            }

            // Too many web console lines enqueued.
            // Signal dequeue task to run immediately in case of the task is still waiting for deplay
            if (_webConsoleLineQueue.Count >= _webConsoleLineQueueForceProcessThreshold)
            {
                Trace.Verbose("Web console line queue has {0} lines enqueued, reach force process threshold {1}, signal dequeue task to process them right now.", _webConsoleLineQueue.Count, _webConsoleLineQueueForceProcessThreshold);
                if (_webConsoleLineQueueSemaphore.CurrentCount == 0)
                {
                    _webConsoleLineQueueSemaphore.Release();
                }
            }
        }

        public void QueueFileUpload(Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource)
        {
            if (timelineId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(timelineId));
            }

            if (timelineRecordId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(timelineRecordId));
            }

            // all parameter not null, file path exist.
            lock (_fileUploadQueueLock)
            {
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

            // Too many file upload enqueued.
            // Signal dequeue task to run immediately in case of the task is still waiting for deplay
            if (_fileUploadQueue.Count >= _fileUploadQueueForceProcessThreshold)
            {
                Trace.Verbose("file upload queue has {0} files enqueued, reach force process threshold {1}, signal dequeue task to process them right now.", _fileUploadQueue.Count, _fileUploadQueueForceProcessThreshold);
                if (_fileUploadQueueSemaphore.CurrentCount == 0)
                {
                    _fileUploadQueueSemaphore.Release();
                }
            }
        }

        public void QueueTimelineRecordUpdate(Guid timelineId, TimelineRecord timelineRecord)
        {
            if (timelineId == Guid.Empty ||
                timelineRecord == null || 
                timelineRecord.Id == Guid.Empty)
            {
                // TODO: throw
                return;
            }

            lock (_timelineUpdateQueueLock)
            {
                Queue<TimelineRecord> recordQueue;
                if (!_timelineUpdateQueue.TryGetValue(timelineId, out recordQueue))
                {
                    _timelineUpdateQueue[timelineId] = new Queue<TimelineRecord>();
                }

                Trace.Verbose("Enqueue timeline {0} update queue: {1}", timelineId, timelineRecord.Id);
                _timelineUpdateQueue[timelineId].Enqueue(timelineRecord.Clone());
            }

            // Too many timeline update enqueued.
            // Signal dequeue task to run immediately in case of the task is still waiting for deplay
            var totalPendingCount = _timelineUpdateQueue.Sum(q => q.Value.Count);
            if (totalPendingCount >= _timelineUpdateQueueForceProcessThreshold)
            {
                Trace.Verbose("timeline update queue has {0} updates enqueued for all timeline, reach force process threshold {1}, signal dequeue task to process them right now.", totalPendingCount, _timelineUpdateQueueForceProcessThreshold);
                if (_timelineUpdateQueueSemaphore.CurrentCount == 0)
                {
                    _timelineUpdateQueueSemaphore.Release();
                }
            }
        }

        private async Task ProcessWebConsoleLinesQueueAsync(bool runOnce = false)
        {
            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                if (!runOnce)
                {
                    bool forceProcess = await _webConsoleLineQueueSemaphore.WaitAsync(_delayForWebConsoleLineDequeue);
                    if (forceProcess)
                    {
                        Trace.Verbose("Process web console line queue since the force process signal been fired.");
                    }
                    else
                    {
                        Trace.Verbose("Process web console line queue since the time delay has met.");
                    }
                }

                List<List<string>> batchedLines = new List<List<string>>();
                lock (_webConsoleLineQueueLock)
                {
                    List<string> currentBatch = new List<string>();
                    while (_webConsoleLineQueue.Count > 0)
                    {
                        string line = _webConsoleLineQueue.Dequeue();
                        currentBatch.Add(line);

                        if (currentBatch.Count > 100)
                        {
                            batchedLines.Add(currentBatch.ToList());
                            currentBatch.Clear();
                        }
                    }

                    if (currentBatch.Count > 0)
                    {
                        batchedLines.Add(currentBatch.ToList());
                        currentBatch.Clear();
                    }
                }

                if (batchedLines.Count > 0)
                {
                    List<Exception> webConsoleLinePostExceptions = new List<Exception>();
                    foreach (var batch in batchedLines)
                    {
                        try
                        {
                            // we will not requeue failed batch, since the web console lines are time sensitive.
                            await _jobServer.AppendTimelineRecordFeedAsync(_scopeIdentifier, _hubName, _planId, _jobTimelineId, _jobTimelineRecordId, batch, default(CancellationToken));
                        }
                        catch (Exception ex)
                        {
                            Trace.Verbose("Catch exception during append web console line, keep going since the process is best effort. Due with exception when all batches finish.");
                            webConsoleLinePostExceptions.Add(ex);
                        }
                    }

                    Trace.Info("Try to append {0} batches web console lines, success rate: {1}/{0}.", batchedLines.Count, batchedLines.Count - webConsoleLinePostExceptions.Count);

                    if (webConsoleLinePostExceptions.Count > 0)
                    {
                        AggregateException ex = new AggregateException("Catch exception during append web console line.", webConsoleLinePostExceptions);
                        if (!runOnce)
                        {
                            Trace.Verbose("Catch exception during process web console line queue, keep going since the process is best effort.");
                            Trace.Error(ex);
                        }
                        else
                        {
                            Trace.Error("Catch exception during drain web console line queue. throw aggregate exception to caller.");
                            throw ex;
                        }
                    }
                }

                if (runOnce)
                {
                    break;
                }
            }
        }

        private async Task ProcessFilesUploadQueueAsync(bool runOnce = false)
        {
            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                if (!runOnce)
                {
                    bool forceProcess = await _fileUploadQueueSemaphore.WaitAsync(_delayForFileUploadDequeue);
                    if (forceProcess)
                    {
                        Trace.Verbose("Process file upload queue since the force process signal been fired.");
                    }
                    else
                    {
                        Trace.Verbose("Process file upload queue since the time delay has met.");
                    }
                }

                List<UploadFileInfo> filesToUpload = new List<UploadFileInfo>();
                lock (_fileUploadQueueLock)
                {
                    while (_fileUploadQueue.Count > 0)
                    {
                        UploadFileInfo file = _fileUploadQueue.Dequeue();
                        filesToUpload.Add(file);
                    }
                }

                if (filesToUpload.Count > 0)
                {
                    // TODO: upload all file in parallel
                    List<Exception> fileUploadExceptions = new List<Exception>();
                    foreach (var file in filesToUpload)
                    {
                        try
                        {
                            await UploadFile(file);
                        }
                        catch (Exception ex)
                        {
                            Trace.Verbose("Catch exception during log or attachment file upload, keep going since the process is best effort. Due with exception when all files upload.");
                            fileUploadExceptions.Add(ex);

                            // put the failed upload file back to queue.
                            // TODO: figure out how should we retry paging log upload.
                            //lock (_fileUploadQueueLock)
                            //{
                            //    _fileUploadQueue.Enqueue(file);
                            //}
                        }
                    }

                    Trace.Info("Try to upload {0} log files or attachments, success rate: {1}/{0}.", filesToUpload.Count, filesToUpload.Count - fileUploadExceptions.Count);

                    if (fileUploadExceptions.Count > 0)
                    {
                        AggregateException ex = new AggregateException("Catch exception during upload log file or attachment.", fileUploadExceptions);
                        if (!runOnce)
                        {
                            Trace.Verbose("Catch exception during process file upload queue, keep going since the process is best effort.");
                            Trace.Error(ex);
                        }
                        else
                        {
                            Trace.Error("Catch exception during drain file upload queue queue. throw aggregate exception to caller.");
                            throw ex;
                        }
                    }
                }

                if (runOnce)
                {
                    break;
                }
            }
        }

        private async Task ProcessTimelinesUpdateQueueAsync(bool runOnce = false)
        {
            while (!_jobCompletionSource.Task.IsCompleted || runOnce)
            {
                if (!runOnce)
                {
                    bool forceProcess = await _timelineUpdateQueueSemaphore.WaitAsync(_delayForTimelineUpdateDequeue);
                    if (forceProcess)
                    {
                        Trace.Verbose("Process timeline update queue since the force process signal been fired.");
                    }
                    else
                    {
                        Trace.Verbose("Process timeline update queue since the time delay has met.");
                    }
                }

                List<PendingTimelineRecord> pendingUpdates = new List<PendingTimelineRecord>();
                lock (_timelineUpdateQueueLock)
                {
                    foreach (var timeline in _allTimelines)
                    {
                        Queue<TimelineRecord> recordQueue;
                        if (_timelineUpdateQueue.TryGetValue(timeline, out recordQueue))
                        {
                            List<TimelineRecord> records = new List<TimelineRecord>();
                            while (recordQueue.Count > 0)
                            {
                                TimelineRecord rec = recordQueue.Dequeue();
                                records.Add(rec);
                            }

                            if (records.Count > 0)
                            {
                                pendingUpdates.Add(new PendingTimelineRecord() { TimelineId = timeline, PendingRecords = records.ToList() });
                            }
                        }
                        else
                        {
                            //TODO: trace, this should never happen.
                        }
                    }
                }

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
                            if (!_allTimelines.Contains(detailTimeline.Details.Id))
                            {
                                try
                                {
                                    Timeline newTimeline = await _jobServer.CreateTimelineAsync(_scopeIdentifier, _hubName, _planId, detailTimeline.Details.Id, default(CancellationToken));
                                    _allTimelines.Add(newTimeline.Id);
                                }
                                // catch timeline exist exception
                                catch (Exception ex)
                                {
                                    Trace.Error(ex);
                                }
                            }
                        }

                        try
                        {
                            await _jobServer.UpdateTimelineRecordsAsync(_scopeIdentifier, _hubName, _planId, update.TimelineId, update.PendingRecords, default(CancellationToken));
                            if (_bufferedRetryRecords.Remove(update.TimelineId))
                            {
                                Trace.Verbose("Cleanup buffered timeline record for timeline: {0}.", update.TimelineId);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.Error("Catch exception during update timeline records, try to update these timeline records next time.");
                            Trace.Error(ex);
                            _bufferedRetryRecords[update.TimelineId] = update.PendingRecords.ToList();
                        }
                    }
                }

                if (runOnce)
                {
                    break;
                }
            }
        }

        private List<TimelineRecord> MergeTimelineRecords(List<TimelineRecord> timelineRecords)
        {
            if (timelineRecords == null || timelineRecords.Count <= 1)
            {
                return timelineRecords;
            }

            Dictionary<Guid, TimelineRecord> dict = new Dictionary<Guid, TimelineRecord>();
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
                    timelineRecord.PercentComplete = rec.PercentComplete ?? timelineRecord.PercentComplete;
                    timelineRecord.RecordType = rec.RecordType ?? timelineRecord.RecordType;
                    timelineRecord.Result = rec.Result ?? timelineRecord.Result;
                    timelineRecord.ResultCode = rec.ResultCode ?? timelineRecord.ResultCode;
                    timelineRecord.StartTime = rec.StartTime ?? timelineRecord.StartTime;
                    timelineRecord.State = rec.State ?? timelineRecord.State;
                    timelineRecord.WorkerName = rec.WorkerName ?? timelineRecord.WorkerName;
                    timelineRecord.ErrorCount = rec.ErrorCount ?? timelineRecord.ErrorCount;
                    timelineRecord.WarningCount = rec.WarningCount ?? timelineRecord.WarningCount;
                    if (rec.Issues.Count > 0)
                    {
                        timelineRecord.Issues.Clear();
                        timelineRecord.Issues.AddRange(rec.Issues.Select(i => i.Clone()));
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
            }

            return mergedRecords;
        }

        private async Task UploadFile(UploadFileInfo file)
        {
            bool uploadSucceed = false;
            try
            {
                if (String.Equals(file.Type, CoreAttachmentType.Log, StringComparison.OrdinalIgnoreCase))
                {
                    // Create the log
                    var taskLog = await _jobServer.CreateLogAsync(_scopeIdentifier, _hubName, _planId, new TaskLog(String.Format(@"logs\{0:D}", file.TimelineRecordId)), default(CancellationToken));

                    // Upload the contents
                    using (FileStream fs = File.OpenRead(file.Path))
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
                    using (FileStream fs = File.OpenRead(file.Path))
                    {
                        var result = await _jobServer.CreateAttachmentAsync(_scopeIdentifier, _hubName, _planId, file.TimelineId, file.TimelineRecordId, file.Type, file.Name, fs, default(CancellationToken));
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
                        Trace.Verbose("Catch exception during delete success uploaded file.");
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
}