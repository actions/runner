using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class TimeLineWebConsoleLogController : VssControllerBase
    {

        private IMemoryCache _cache;

        public TimeLineWebConsoleLogController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet("{timelineId}/{recordId}")]
        public IEnumerable<TimelineRecordLogLine> GetLogLines(Guid timelineId, Guid recordId)
        {
            if(TimelineController.dict.TryGetValue(timelineId, out var rec) && rec.Item2.TryGetValue(recordId, out var value)) {
                return from line in value where line != null select line;
            }
            return null;
        }

        [HttpGet("{timelineId}")]
        public ConcurrentDictionary<Guid, List<TimelineRecordLogLine>> GetLogLines(Guid timelineId)
        {
            if(TimelineController.dict.TryGetValue(timelineId, out var rec)) {
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

        public delegate void LogFeedEvent(object sender, Guid timelineId, Guid recordId, TimelineRecordFeedLinesWrapper record);
        public static event LogFeedEvent logfeed;

        [HttpGet]
        public IActionResult Message([FromQuery] Guid timelineId, [FromQuery] long[] runid)
        {
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                var writer = new StreamWriter(stream);
                try
                {
                    writer.NewLine = "\n";
                    var queue2 = Channel.CreateUnbounded<KeyValuePair<string,string>>(new UnboundedChannelOptions { SingleReader = true });
                    var chwriter = queue2.Writer;
                    LogFeedEvent handler = (sender, timelineId2, recordId, record) => {
                        (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) val;
                        Job job;
                        if (timelineId == timelineId2 || timelineId == Guid.Empty && (runid.Length == 0 || TimelineController.dict.TryGetValue(timelineId2, out val) && _cache.TryGetValue(val.Item1[0].Id, out job) && runid.Contains(job.runid))) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("log", JsonConvert.SerializeObject(new { timelineId = timelineId2, recordId, record }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };
                    TimelineController.TimeLineUpdateDelegate handler2 = (timelineId2, timeline) => {
                        (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) val;
                        Job job;
                        if(timelineId == timelineId2 || timelineId == Guid.Empty && (runid.Length == 0 || TimelineController.dict.TryGetValue(timelineId2, out val) && _cache.TryGetValue(val.Item1[0].Id, out job) && runid.Contains(job.runid))) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("timeline", JsonConvert.SerializeObject(new { timelineId = timelineId2, timeline }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };
                    MessageController.RepoDownload rd = (_runid, url, submodules, nestedSubmodules) => {
                        if(runid.Contains(_runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("repodownload", JsonConvert.SerializeObject(new { url, submodules, nestedSubmodules }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };

                    FinishJobController.JobCompleted completed = (ev) => {
                        Job job;
                        if(runid.Length == 0 || (_cache.TryGetValue(ev.JobId, out job) && runid.Contains(job.runid))) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("finish", JsonConvert.SerializeObject(ev, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };

                    Action<MessageController.WorkflowEventArgs> workflow = workflow_ => {
                        if(runid.Contains(workflow_.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("workflow", JsonConvert.SerializeObject(workflow_, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };
                    
                    var chreader = queue2.Reader;
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, string> p = await chreader.ReadAsync(requestAborted);
                                await writer.WriteLineAsync($"event: {p.Key}");
                                await writer.WriteLineAsync($"data: {p.Value}");
                                await writer.WriteLineAsync();
                                await writer.FlushAsync();
                            }
                        } catch (OperationCanceledException) {

                        }
                    }, requestAborted);
                    try {
                        logfeed += handler;
                        TimelineController.TimeLineUpdate += handler2;
                        MessageController.OnRepoDownload += rd;
                        FinishJobController.OnJobCompleted += completed;
                        MessageController.workflowevent += workflow;
                        await ping;
                    } finally {
                        logfeed -= handler;
                        TimelineController.TimeLineUpdate -= handler2;
                        MessageController.OnRepoDownload -= rd;
                        FinishJobController.OnJobCompleted -= completed;
                        MessageController.workflowevent -= workflow;
                    }
                } catch (OperationCanceledException) {

                } finally {
                    await writer.DisposeAsync();
                }
            }, "text/event-stream");
        }

        public static void AppendTimelineRecordFeed(TimelineRecordFeedLinesWrapper record, Guid timelineId, Guid recordId) {
            logfeed?.Invoke(null, timelineId, recordId, record);
            (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) timeline;
            timeline = TimelineController.dict.GetOrAdd(timelineId, g => (new List<TimelineRecord>(), new ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>()));
            timeline.Item2.AddOrUpdate(record.StepId, t => record.Value.Select((s, i) => new TimelineRecordLogLine(s, null)).ToList(), (g, t) => {
                t.AddRange(record.Value.Select((s) => new TimelineRecordLogLine(s, null)));
                return t;
            });
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/{timelineId}/{recordId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
        public async Task<IActionResult> AppendTimelineRecordFeed(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid recordId)
        {
            var record = await FromBody<TimelineRecordFeedLinesWrapper>();
            Task.Run(() => AppendTimelineRecordFeed(record, timelineId, recordId));
            return Ok();
        }
    }
}
