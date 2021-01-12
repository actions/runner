using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    public class FinishJobController : ControllerBase
    {

        private readonly ILogger<FinishJobController> _logger;

        public FinishJobController(ILogger<FinishJobController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public void OnEvent(Guid scopeIdentifier, string hubName, Guid planId, [FromBody] JobEvent jevent)
        {
            if (jevent is JobCompletedEvent ev) {
                MessageController.queueLock.WaitOne();
                try {
                    MessageController.dict.Remove(MessageController.jobIdToSessionId[ev.JobId]);
                    MessageController.jobIdToSessionId.Remove(ev.JobId);
                    Console.Out.WriteLine("Job finished");
                } finally {
                    MessageController.queueLock.ReleaseMutex();
                }
            } else if (jevent is JobAssignedEvent a) {
                Console.Out.WriteLine("Job assigned");
            } else if(jevent is JobStartedEvent s) {
                
            }
        }
    }
}
