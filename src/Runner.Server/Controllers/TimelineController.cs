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

        public void SyncLiveLogsToDb(Guid timelineId) {
            if(dict.TryGetValue(timelineId, out var entry)) {
                foreach(var rec in (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList()) {
                    if(rec.Log == null && entry.Item2.TryGetValue(rec.Id, out var value)) {
                        var log = new TaskLog() {  };
                        _context.Logs.Add(new SqLiteDb.LogStorage() { Ref = log, Content = string.Join('\n', from line in value where line != null select line.Line) });
                        rec.Log = log;
                    }
                }
                _context.SaveChanges();
                dict.TryRemove(timelineId, out _);
            }
        }

        [HttpGet("{timelineId}")]
        public async Task<IActionResult> GetTimelineRecords(Guid timelineId) {
            var l = (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList();
            l.Sort((a,b) => a.ParentId == null ? -1 : b.ParentId == null ? 1 : a.Order - b.Order ?? 0);
            return await Ok(l, true);
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/timeline")]
        public async Task<IActionResult> CreateTimeline() {
            var timeline = await FromBody<Timeline>();
            return await Ok(timeline);
        }

        [HttpGet("{scopeIdentifier}/{hubName}/{planId}/timeline/{timelineid}")]
        public async Task<IActionResult> GetTimeline(Guid timelineId) {
            var timeline = new Timeline(timelineId);
            var l = (from record in _context.TimeLineRecords where record.TimelineId == timelineId select record).Include(r => r.Log).ToList();
            l.Sort((a,b) => a.ParentId == null ? -1 : b.ParentId == null ? 1 : a.Order - b.Order ?? 0);
            timeline.Records.AddRange(l);
            return await Ok(timeline);
        }

        [HttpPut("{scopeIdentifier}/{hubName}/{planId}/{timelineId}/attachments/{recordId}/{type}/{name}")]
        public async Task<IActionResult> PutAttachment(Guid timelineId, Guid recordId, string type, string name) {
            var jobInfo = (from j in _context.Jobs where j.TimeLineId == timelineId select new { j.runid, j.Attempt }).FirstOrDefault();
            var artifacts = new ArtifactController(_context, Configuration);
            var fname = $"Attachment_{timelineId}_{recordId}_{type}";
            var container = await artifacts.CreateContainer(jobInfo.runid, jobInfo.Attempt, new CreateActionsStorageArtifactParameters() { Name = fname });
            var record = new ArtifactRecord() {FileName = Path.Join(fname, name), StoreName = Path.GetRandomFileName(), GZip = false, FileContainer = container} ;
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
        public async Task<IActionResult> Patch(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId)
        {
            var patch = await FromBody<VssJsonCollectionWrapper<List<TimelineRecord>>>();
            return await UpdateTimeLine(timelineId, patch);
        }

        public async Task<IActionResult> UpdateTimeLine(Guid timelineId, VssJsonCollectionWrapper<List<TimelineRecord>> patch)
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
            Task.Run(() => TimeLineUpdate?.Invoke(timelineId, records));
            
            await _context.AddRangeAsync(from rec in records where !old.Contains(rec) select rec);
            await _context.SaveChangesAsync();
            return await Ok(patch);
        }
    }
}
