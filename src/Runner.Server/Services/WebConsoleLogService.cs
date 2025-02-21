using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Runner.Server.Models;

namespace Runner.Server.Services
{
    public class WebConsoleLogService
    {
        private readonly ConcurrentDictionary<Guid, (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>)> _dict = new();
        private readonly IServiceProvider _provider;

        public WebConsoleLogService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public delegate void LogFeedEvent(object sender, Guid timelineId, Guid recordId, TimelineRecordFeedLinesWrapper record);
        public event LogFeedEvent LogFeed;
        
        public (Guid, List<TimelineRecord>) CreateNewRecord(Guid timelineId, TimelineRecord record) {
            var list = new List<TimelineRecord> { record };
            _dict[timelineId] = (list, new ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>());
            return (timelineId, list);
        }

        public IEnumerable<TimelineRecordLogLine> GetLogLines(Guid timelineId, Guid recordId)
        {
            if(_dict.TryGetValue(timelineId, out var rec) && rec.Item2.TryGetValue(recordId, out var value)) {
                return from line in value where line != null select line;
            }
            return null;
        }

        public ConcurrentDictionary<Guid, List<TimelineRecordLogLine>> GetLogLines(Guid timelineId)
        {
            if(_dict.TryGetValue(timelineId, out var rec)) {
                return rec.Item2;
            }
            return null;
        }

        public List<TimelineRecord> GetTimeLine(Guid timelineId)
        {
            if(_dict.TryGetValue(timelineId, out var rec)) {
                return rec.Item1;
            }
            return null;
        }

        public void AppendTimelineRecordFeed(TimelineRecordFeedLinesWrapper record, Guid timelineId, Guid recordId) {
            LogFeed?.Invoke(null, timelineId, recordId, record);
            (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) timeline;
            timeline = _dict.GetOrAdd(timelineId, g => (new List<TimelineRecord>(), new ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>()));
            timeline.Item2.AddOrUpdate(record.StepId, t => record.Value.Select((s, i) => new TimelineRecordLogLine(s, null)).ToList(), (g, t) => {
                t.AddRange(record.Value.Select((s) => new TimelineRecordLogLine(s, null)));
                return t;
            });
        }

        public void SyncLiveLogsToDb(Guid timelineId) {
            if(_dict.TryRemove(timelineId, out var entry)) {
                using var scope = _provider.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<SqLiteDb>();
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
    }
}
