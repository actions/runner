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

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class FinishJobController : VssControllerBase
    {
        private IMemoryCache _cache;
        private SqLiteDb _context;

        public FinishJobController(IMemoryCache cache, SqLiteDb context, IConfiguration conf) : base(conf)
        {
            _cache = cache;
            _context = context;
        }

        public delegate void JobCompleted(JobCompletedEvent jobCompletedEvent);
        public delegate void JobAssigned(JobAssignedEvent jobAssignedEvent);
        public delegate void JobStarted(JobStartedEvent jobStartedEvent);

        public static event JobCompleted OnJobCompleted;
        public static event JobCompleted OnJobCompletedAfter;
        public static event JobAssigned OnJobAssigned;
        public static event JobStarted OnJobStarted;

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
                        new TimelineController(_context, Configuration).SyncLiveLogsToDb(job.TimeLineId);
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
            } else if (jevent is JobAssignedEvent a) {
                Task.Run(() => OnJobAssigned?.Invoke(a));
                return Ok();
            } else if(jevent is JobStartedEvent s) {
                Task.Run(() => OnJobStarted?.Invoke(s));
                return Ok();
            }
            return NotFound();
        }
    }
}
