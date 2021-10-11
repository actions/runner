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

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class FinishJobController : VssControllerBase
    {
        private IMemoryCache _cache;

        public FinishJobController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        public delegate void JobCompleted(JobCompletedEvent jobCompletedEvent);
        public delegate void JobAssigned(JobAssignedEvent jobAssignedEvent);
        public delegate void JobStarted(JobStartedEvent jobStartedEvent);

        public static event JobCompleted OnJobCompleted;
        public static event JobCompleted OnJobCompletedAfter;
        public static event JobAssigned OnJobAssigned;
        public static event JobStarted OnJobStarted;

        public static void InvokeJobCompleted(JobCompletedEvent e) {
            OnJobCompleted?.Invoke(e);
            OnJobCompletedAfter?.Invoke(e);
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> OnEvent(Guid scopeIdentifier, string hubName, Guid planId)
        {
            var jevent = await FromBody<JobEvent>();
            if (jevent is JobCompletedEvent ev) {
                MessageController.Job job;
                if(_cache.TryGetValue(ev.JobId, out job)) {
                    job.JobCompletedEvent = ev;
                    Session session;
                    if(_cache.TryGetValue(job.SessionId, out session)) {
                        Console.Out.WriteLine("Job finished / set session job to null");
                        session.Job = null;
                    }
                }
                Task.Run(() => {
                    OnJobCompleted?.Invoke(ev);
                    OnJobCompletedAfter?.Invoke(ev);
                });
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
