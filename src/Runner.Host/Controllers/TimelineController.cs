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

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    public class TimelineController : ControllerBase
    {
        List<TimelineRecord> records = new List<TimelineRecord>();

        private readonly ILogger<TimelineController> _logger;

        public TimelineController(ILogger<TimelineController> logger)
        {
            _logger = logger;
        }

        [HttpPatch("{scopeIdentifier}/{hubName}/{planId}/{timelineId}")]
        public VssJsonCollectionWrapper<List<TimelineRecord>> Patch(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, [FromBody] VssJsonCollectionWrapper<IEnumerable<TimelineRecord>> patch)
        {

            var record = new VssJsonCollectionWrapper<List<TimelineRecord>>(patch.Value);
            // patch.ApplyTo(record);
            return record;
        }
    }
}
