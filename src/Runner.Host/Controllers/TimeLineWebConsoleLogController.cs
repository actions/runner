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
    public class TimeLineWebConsoleLogController : ControllerBase
    {

        private readonly ILogger<TimeLineWebConsoleLogController> _logger;

        private List<TaskAgentPool> pool = new List<TaskAgentPool> {
            new TaskAgentPool("Agents") {
                Id = 1,
                IsHosted = false,
                IsInternal = true
            }
        };

        public TimeLineWebConsoleLogController(ILogger<TimeLineWebConsoleLogController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/{timelineId}/{recordId}")]
        public void AppendTimelineRecordFeed(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid recordId, [FromBody] TimelineRecordFeedLinesWrapper record)
        {
            
        }

        
    }
}
