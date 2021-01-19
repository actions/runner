using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Runner.Host.Models;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("runner/host/_apis/v1/[controller]")]
    public class TimelineController : VssControllerBase
    {
        // private readonly InMemoryDB _context;
        private readonly ILogger<TimelineController> _logger;
        public static Dictionary<Guid, (List<TimelineRecord>, Dictionary<Guid, List<TimelineRecordLogLine>>)> dict = new Dictionary<Guid, (List<TimelineRecord>, Dictionary<Guid, List<TimelineRecordLogLine>>)>();

        public TimelineController(ILogger<TimelineController> logger/* , InMemoryDB context */)
        {
            _logger = logger;
            // _context = context;
        }

        [HttpGet("{timelineId}")]
        public IEnumerable<TimelineRecord> GetTimelineRecords(Guid timelineId) {
            return dict[timelineId].Item1;
        }

        // private TimelineRecord PatchRecord(TimelineRecord timelineRecord, TimelineRecord rec) {
        //     timelineRecord.CurrentOperation = rec.CurrentOperation ?? timelineRecord.CurrentOperation;
        //     timelineRecord.Details = rec.Details ?? timelineRecord.Details;
        //     timelineRecord.FinishTime = rec.FinishTime ?? timelineRecord.FinishTime;
        //     timelineRecord.Log = rec.Log ?? timelineRecord.Log;
        //     timelineRecord.Name = rec.Name ?? timelineRecord.Name;
        //     timelineRecord.RefName = rec.RefName ?? timelineRecord.RefName;
        //     timelineRecord.PercentComplete = rec.PercentComplete ?? timelineRecord.PercentComplete;
        //     timelineRecord.RecordType = rec.RecordType ?? timelineRecord.RecordType;
        //     timelineRecord.Result = rec.Result ?? timelineRecord.Result;
        //     timelineRecord.ResultCode = rec.ResultCode ?? timelineRecord.ResultCode;
        //     timelineRecord.StartTime = rec.StartTime ?? timelineRecord.StartTime;
        //     timelineRecord.State = rec.State ?? timelineRecord.State;
        //     timelineRecord.WorkerName = rec.WorkerName ?? timelineRecord.WorkerName;

        //     if (rec.ErrorCount != null && rec.ErrorCount > 0)
        //     {
        //         timelineRecord.ErrorCount = rec.ErrorCount;
        //     }

        //     if (rec.WarningCount != null && rec.WarningCount > 0)
        //     {
        //         timelineRecord.WarningCount = rec.WarningCount;
        //     }

        //     if (rec.Issues.Count > 0)
        //     {
        //         timelineRecord.Issues.Clear();
        //         timelineRecord.Issues.AddRange(rec.Issues.Select(i => i.Clone()));
        //     }

        //     if (rec.Variables.Count > 0)
        //     {
        //         foreach (var variable in rec.Variables)
        //         {
        //             timelineRecord.Variables[variable.Key] = variable.Value.Clone();
        //         }
        //     }
        // }

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

            // Trace.Verbose("Merged Timeline records");
            // foreach (var record in mergedRecords)
            // {
            //     Trace.Verbose($"    Record: t={record.RecordType}, n={record.Name}, s={record.State}, st={record.StartTime}, {record.PercentComplete}%, ft={record.FinishTime}, r={record.Result}: {record.CurrentOperation}");
            //     if (record.Issues != null && record.Issues.Count > 0)
            //     {
            //         foreach (var issue in record.Issues)
            //         {
            //             String source;
            //             issue.Data.TryGetValue("sourcepath", out source);
            //             Trace.Verbose($"        Issue: c={issue.Category}, t={issue.Type}, s={source ?? string.Empty}, m={issue.Message}");
            //         }
            //     }

            //     if (record.Variables != null && record.Variables.Count > 0)
            //     {
            //         foreach (var variable in record.Variables)
            //         {
            //             Trace.Verbose($"        Variable: n={variable.Key}, secret={variable.Value.IsSecret}");
            //         }
            //     }
            // }

            return mergedRecords;
        }

        [HttpPatch("{scopeIdentifier}/{hubName}/{planId}/{timelineId}")]
        public async Task<IActionResult> Patch(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId)
        {
            var patch = await FromBody<VssJsonCollectionWrapper<List<TimelineRecord>>>();
            var compare = new TimelineRecord();
            if(!dict.TryAdd(timelineId, (patch.Value, new Dictionary<Guid, List<TimelineRecordLogLine>>()))) {
                dict[timelineId].Item1.AddRange(patch.Value);
                dict[timelineId] = (MergeTimelineRecords(dict[timelineId].Item1), dict[timelineId].Item2);
            }
            return await Ok(patch);
        //     if(patch.Count > 0) {
        //         var a = patch.Value.ToArray();
        //         foreach (var item in a)
        //         {
        //             item.TimelineId = timelineId;
        //         }
        //         if(_context.Set<TimelineRecord>().Any(rec => rec.TimelineId == timelineId)) {
        //             _context.Set<TimelineRecord>().UpdateRange(a);
        //         } else {
        //             await _context.Set<TimelineRecord>().AddRangeAsync(a);
        //         }
        //         await _context.SaveChangesAsync();
        //     }
        //     // var recs = _context.Set<TimelineRecord>().ToArray();
        //     var record = new VssJsonCollectionWrapper<IEnumerable<TimelineRecord>>(from rec in _context.Set<TimelineRecord>() where rec.TimelineId == timelineId select rec);
        //     // patch.ApplyTo(record);
        //     return record;
        }
    }
}
