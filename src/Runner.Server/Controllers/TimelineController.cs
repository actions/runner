using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Runner.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using GitHub.Actions.Pipelines.WebApi;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class TimelineController : VssControllerBase
    {
        public static ConcurrentDictionary<Guid, (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>)> dict = new ConcurrentDictionary<Guid, (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>)>();
        private SqLiteDb _context;

        public TimelineController(SqLiteDb context, IConfiguration conf) : base(conf)
        {
            _context = context;
        }

        internal void SyncLiveLogsToDb(Guid timelineId) {
            if(dict.TryRemove(timelineId, out var entry)) {
                foreach(var rec in (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList()) {
                    if(rec.Log == null && entry.Item2.TryGetValue(rec.Id, out var value)) {
                        var log = new TaskLog() {  };
                        _context.Logs.Add(new SqLiteDb.LogStorage() { Ref = log, Content = string.Join('\n', from line in value where line != null select line.Line) });
                        rec.Log = log;
                    }
                }
                _context.SaveChanges();
            }
        }

        [HttpGet("{timelineId}")]
        public async Task<IActionResult> GetTimelineRecords(Guid timelineId) {
            var l = (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList();
            l.Sort((a,b) => a.ParentId == null ? -1 : b.ParentId == null ? 1 : a.Order - b.Order ?? 0);
            foreach(var record in l) {
                record.Issues.AddRange((from item in _context.TimelineIssues where item.Record == record select item).ToList().Select(item => {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.Data);
                    var issue = new Issue { Category = item.Category, IsInfrastructureIssue = item.IsInfrastructureIssue, Message = item.Message, Type = item.Type };
                    foreach(var kv in data) {
                        issue.Data[kv.Key] = kv.Value;
                    }
                    return issue;
                }));
                foreach(var variable in from item in _context.TimelineVariables where item.Record == record select item) {
                    record.Variables[variable.Name] = new VariableValue { Value = variable.Value };
                }
            }
            return await Ok(l, true);
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/timeline")]
        [SwaggerResponse(200, type: typeof(Timeline))]
        public async Task<IActionResult> CreateTimeline([FromBody, Vss] Timeline timeline) {
            return await Ok(timeline);
        }

        [HttpGet("{scopeIdentifier}/{hubName}/{planId}/timeline/{timelineid}")]
        [SwaggerResponse(200, type: typeof(Timeline))]
        public async Task<IActionResult> GetTimeline(Guid timelineId) {
            var timeline = new Timeline(timelineId);
            var l = (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList();
            l.Sort((a,b) => a.ParentId == null ? -1 : b.ParentId == null ? 1 : a.Order - b.Order ?? 0);
            timeline.Records.AddRange(l);
            return await Ok(timeline);
        }

        [HttpPut("{scopeIdentifier}/{hubName}/{planId}/{timelineId}/attachments/{recordId}/{type}/{name}")]
        [SwaggerResponse(200, type: typeof(TaskAttachment))]
        public async Task<IActionResult> PutAttachment(Guid timelineId, Guid recordId, string type, string name) {
            var jobInfo = (from j in _context.Jobs where j.TimeLineId == timelineId select new { j.runid, j.Attempt }).FirstOrDefault();
            var artifacts = new ArtifactController(_context, Configuration);
            var prefix = $"Attachment_{timelineId}_{recordId}";
            var fname = $"{prefix}_{type}_{name}";
            var container = await artifacts.CreateContainer(jobInfo.runid, jobInfo.Attempt, new CreateActionsStorageArtifactParameters() { Name = prefix });
            var record = new ArtifactRecord() {FileName = fname, StoreName = Path.GetRandomFileName(), GZip = false, FileContainer = container} ;
            _context.ArtifactRecords.Add(record);
            await _context.SaveChangesAsync();
            var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
            using(var targetStream = new FileStream(Path.Combine(_targetFilePath, record.StoreName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                await Request.Body.CopyToAsync(targetStream);
            }
            return await Ok(new TaskAttachment(type, name) { RecordId = recordId, TimelineId = timelineId });
        }
        
        
        public delegate void TimeLineUpdateDelegate(Guid timelineId, List<TimelineRecord> update);
        public static event TimeLineUpdateDelegate TimeLineUpdate;

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
            mergedRecords.Sort((a,b) => a.ParentId == null ? -1 : b.ParentId == null ? 1 : a.Order - b.Order ?? 0);
            return mergedRecords;
        }

        [HttpPatch("{scopeIdentifier}/{hubName}/{planId}/{timelineId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
        [SwaggerResponse(200, type: typeof(VssJsonCollectionWrapper<List<TimelineRecord>>))]
        public async Task<IActionResult> Patch(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, [FromBody, Vss] VssJsonCollectionWrapper<List<TimelineRecord>> patch)
        {
            return await UpdateTimeLine(timelineId, patch, true);
        }

        internal async Task<IActionResult> UpdateTimeLine(Guid timelineId, VssJsonCollectionWrapper<List<TimelineRecord>> patch, bool outOfSyncTimeLineUpdate = false)
        {
            var old = (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList();
            var records = old.ToList();
            records.AddRange(patch.Value.Select((r, _) => {
                r.TimelineId = timelineId;
                if(r.Log != null) {
                    var logId = r.Log.Id;
                    r.Log = (from l in _context.Set<TaskLogReference>() where l.Id == logId select l).First();
                }
                return r;
            }));
            records = MergeTimelineRecords(records);
            foreach(var r in records) {
                // Bug Detail TimeLine Updates not supported
                if(r.Details != null) {
                    r.Details = null;
                }
                var stored = (from va in _context.TimelineVariables where va.Record == r select va).ToDictionary(kv => kv.Name, kv => kv, StringComparer.OrdinalIgnoreCase);
                foreach(var item in r.Variables)
                {
                    if(stored.TryGetValue(item.Key, out var entry)) {
                        entry.Value = item.Value.Value;
                    } else {
                        _context.TimelineVariables.Add(new TimelineVariable() { Record = r , Name = item.Key, Value = item.Value.Value });
                    }
                }
                var storedIssues = (from va in _context.TimelineIssues where va.Record == r select va).ToDictionary(kv => kv.Message + kv.Data, kv => kv);
                foreach(var item in r.Issues)
                {
                    var data = JsonConvert.SerializeObject(item.Data);
                    if(!storedIssues.ContainsKey(item.Message + data)) {
                        _context.TimelineIssues.Add(new TimelineIssue() { Record = r , Category = item.Category, Data = data, IsInfrastructureIssue = item.IsInfrastructureIssue, Message = item.Message, Type = item.Type });
                    }
                }
            }
            if(outOfSyncTimeLineUpdate) {
                // Delay by async Task.Run caused missing log lines for (reusable) workflows and skipped job logs in Runner.Client
                Task.Run(() => TimeLineUpdate?.Invoke(timelineId, records));
            } else {
                TimeLineUpdate?.Invoke(timelineId, records);
            }
            
            await _context.AddRangeAsync(from rec in records where !old.Contains(rec) select rec);
            await _context.SaveChangesAsync();
            return await Ok(patch);
        }
    }
}
