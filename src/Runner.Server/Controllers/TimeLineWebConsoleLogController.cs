using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class TimeLineWebConsoleLogController : VssControllerBase
    {

        private readonly ILogger<TimeLineWebConsoleLogController> _logger;

        public TimeLineWebConsoleLogController(ILogger<TimeLineWebConsoleLogController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{timelineId}/{recordId}")]
        public IEnumerable<TimelineRecordLogLine> GetLogLines(Guid timelineId, Guid recordId)
        {
            (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) rec; 
            if(TimelineController.dict.TryGetValue(timelineId, out rec)) {
                List<TimelineRecordLogLine> value;
                if(rec.Item2.TryGetValue(recordId, out value)) {
                    return value;
                }
            }
            return null;
        }

        [HttpGet("{timelineId}")]
        public ConcurrentDictionary<Guid, List<TimelineRecordLogLine>> GetLogLines(Guid timelineId)
        {
            (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) rec; 
            if(TimelineController.dict.TryGetValue(timelineId, out rec)) {
                return rec.Item2;
            }
            return null;
        }

        public class PushStreamResult: IActionResult
        {
            private readonly Func<Stream, Task> _onStreamAvailabe;
            private readonly string _contentType;

            public PushStreamResult(Func<Stream, Task> onStreamAvailabe, string contentType)
            {
                _onStreamAvailabe = onStreamAvailabe;
                _contentType = contentType;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                var stream = context.HttpContext.Response.Body;
                context.HttpContext.Response.GetTypedHeaders().ContentType = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
                await _onStreamAvailabe(stream);
            }
        }

        private delegate void LogFeedEvent(object sender, Guid timelineId, Guid recordId, TimelineRecordFeedLinesWrapper record);
        private static event LogFeedEvent logfeed;

        // private struct JsonRecord
        // {
        //     Guid timelineId;
        //     Guid recordId;
        //     TimelineRecordFeedLinesWrapper record;
        // }

        public class LowercaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToLower();
            }
        }

        [HttpGet]
        public IActionResult Message([FromQuery] Guid timelineId)
        {
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                var writer = new StreamWriter(stream);
                try
                {
                    writer.NewLine = "\n";
                    ConcurrentQueue<KeyValuePair<string,string>> queue = new ConcurrentQueue<KeyValuePair<string, string>>();
                    LogFeedEvent handler = (sender, timelineId2, recordId, record) => {
                        if (timelineId == timelineId2 || timelineId == Guid.Empty) {
                            queue.Enqueue(new KeyValuePair<string, string>(timelineId2.ToString(), JsonConvert.SerializeObject(new { timelineId = timelineId2, recordId, record }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };
                    ConcurrentQueue<KeyValuePair<string,string>> queue2 = new ConcurrentQueue<KeyValuePair<string, string>>();
                    TimelineController.TimeLineUpdateDelegate handler2 = (timelineId2, timeline) => {
                        if(timelineId2 == timelineId) {
                            queue2.Enqueue(new KeyValuePair<string, string>("timeline", JsonConvert.SerializeObject(new { timelineId, timeline }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, string> p;
                                if(queue.TryDequeue(out p)) {
                                    await writer.WriteLineAsync("event: log");
                                    await writer.WriteLineAsync(string.Format("data: {0}", p.Value));
                                    await writer.WriteLineAsync();
                                    await writer.FlushAsync();
                                } else if(queue2.TryDequeue(out p)) {
                                    await writer.WriteLineAsync($"event: {p.Key}");
                                    await writer.WriteLineAsync($"data: {p.Value}");
                                    await writer.WriteLineAsync();
                                    await writer.FlushAsync();
                                } else {
                                    await writer.WriteLineAsync("event: ping");
                                    await writer.WriteLineAsync("data: {}");
                                    await writer.WriteLineAsync();
                                    await writer.FlushAsync();
                                    await Task.Delay(1000);
                                }
                            }
                        } catch (OperationCanceledException) {

                        }
                    }, requestAborted);
                    logfeed += handler;
                    TimelineController.TimeLineUpdate += handler2;
                    await ping;
                    logfeed -= handler;
                    TimelineController.TimeLineUpdate -= handler2;
                } finally {
                    await writer.DisposeAsync();
                }
            }, "text/event-stream");
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/{timelineId}/{recordId}")]
        public async Task<IActionResult> AppendTimelineRecordFeed(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid recordId)
        {
            var record = await FromBody<TimelineRecordFeedLinesWrapper>();
            (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) timeline;
            if(TimelineController.dict.ContainsKey(timelineId)) {
                timeline = TimelineController.dict[timelineId];
            } else {
                timeline = (new List<TimelineRecord>(), new ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>());
            }
            if(timeline.Item2.ContainsKey(record.StepId)) {
                timeline.Item2[record.StepId].AddRange(record.Value.Select((s, i) => new TimelineRecordLogLine(s, record.StartLine + i)));
            } else {
                timeline.Item2.TryAdd(record.StepId, record.Value.Select((s, i) => new TimelineRecordLogLine(s, record.StartLine + i)).ToList());
            }
            logfeed?.Invoke(this, timelineId, recordId, record);
            return Ok();
        }

        
    }
}
