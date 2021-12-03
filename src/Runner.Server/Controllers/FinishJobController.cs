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

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class FinishJobController : VssControllerBase
    {
        private IMemoryCache _cache;
        private SqLiteDb _context;

        public FinishJobController(IMemoryCache cache, SqLiteDb context)
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

        public void InvokeJobCompleted(JobCompletedEvent ev) {
            var job = _context.Jobs.Find(ev.JobId);
            if(job != null) {
                if(job.Result != null) {
                    // Prevent overriding job with a result
                    return;
                }
                job.Result = ev.Result;
                if(ev.Outputs != null) {
                    job.Outputs.AddRange(from o in ev.Outputs select new JobOutput { Name = o.Key, Value = o.Value?.Value ?? "" });
                }
                _context.SaveChanges();
                // var context2 = new SqLiteDb(_context.Options);
                // Task.Run(async () => {
                //     var _context = context2;
                    // if(TimelineController.dict.TryGetValue(job.TimeLineId, out var entry)) {
                    //     foreach(var rec in (from record in _context.TimeLineRecords where record.TimelineId == job.TimeLineId select record).Include(r => r.Log).ToList()) {
                    //         if(rec.Log == null)
                    //             _context.Entry(rec).Reference(r => r.Log).Load();
                    //         if(rec.Log == null && entry.Item2.TryGetValue(rec.Id, out var value)) {
                    //             var log = new TaskLog() {  };
                    //             _context.Logs.Add(new SqLiteDb.LogStorage() { Ref = log, Content = string.Join('\n', from line in value where line != null select line.Line) });
                    //             rec.Log = log;
                    //         }
                    //     }
                    //     _context.SaveChanges();
                    //     TimelineController.dict.TryRemove(job.TimeLineId, out _);
                    // }
                // });
            }
            Task.Run(() => {
                OnJobCompleted?.Invoke(ev);
                OnJobCompletedAfter?.Invoke(ev);
            });
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> OnEvent(Guid scopeIdentifier, string hubName, Guid planId)
        {
            var jevent = await FromBody<JobEvent>();
            if (jevent is JobCompletedEvent ev) {
                var job = _cache.Get<Job>(ev.JobId);
                if(job != null) {
                    _cache.Remove(ev.JobId);
                    Session session;
                    if(_cache.TryGetValue(job.SessionId, out session)) {
                        Console.Out.WriteLine("Job finished / set session job to null");
                        session.Job = null;
                    }
                }
                InvokeJobCompleted(ev);
                Console.Out.WriteLine("Job finished");
                return Ok();
            } else if (jevent is JobAssignedEvent a) {
                Task.Run(() => OnJobAssigned?.Invoke(a));
                Console.Out.WriteLine("Job assigned");
                return Ok();
            } else if(jevent is JobStartedEvent s) {
                Task.Run(() => OnJobStarted?.Invoke(s));
                Console.Out.WriteLine("Job started");
                return Ok();
            }
            return NotFound();
        }
    }
}
