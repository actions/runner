using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Runner.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Runner.Server.Services;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class FinishJobController : VssControllerBase
    {
        private IMemoryCache _cache;
        private SqLiteDb _context;
        private WebConsoleLogService _webConsoleLogService;

        public FinishJobController(IMemoryCache cache, SqLiteDb context, IConfiguration conf, WebConsoleLogService webConsoleLogService) : base(conf)
        {
            _cache = cache;
            _context = context;
            _webConsoleLogService = webConsoleLogService;
        }

        public delegate void JobCompleted(JobCompletedEvent jobCompletedEvent);

        public static event JobCompleted OnJobCompleted;
        public static event JobCompleted OnJobCompletedAfter;

        internal void InvokeJobCompleted(JobCompletedEvent ev) {
            try {
                {
                    var job = _cache.Get<Job>(ev.JobId);
                    if(job != null) {
                        Session session;
                        if(_cache.TryGetValue(job.SessionId, out session)) {
                            session.Job = null;
                        }
                    }
                }
                {
                    var job = (from j in _context.Jobs where j.JobId == ev.JobId select j).Include(j => j.WorkflowRunAttempt).FirstOrDefault();
                    if(job != null) {
                        if(job.Result != null) {
                            // Prevent overriding job with a result
                            return;
                        }
                        job.Result = ev.Result;
                        if(ev.Outputs != null) {
                            job.Outputs.AddRange(from o in ev.Outputs select new JobOutput { Name = o.Key, Value = o.Value?.Value ?? "" });
                        } else {
                            ev.Outputs = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
                        }
                        MessageController.UpdateJob(this, job);
                        _context.SaveChanges();
                        _webConsoleLogService.SyncLiveLogsToDb(job.TimeLineId);
                    }
                }
            } finally {
                Task.Run(() => {
                    try {
                        OnJobCompleted?.Invoke(ev);
                        OnJobCompletedAfter?.Invoke(ev);
                    } finally {
                        _cache.Remove(ev.JobId);
                    }
                });
            }
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
        public IActionResult OnEvent(Guid scopeIdentifier, string hubName, Guid planId, [FromBody, Vss] JobEvent jevent)
        {
            if (jevent is JobCompletedEvent ev) {
                InvokeJobCompleted(ev);
                return Ok();
            }
            return NotFound();
        }
    }
}
