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
    public class AgentRequestController : ControllerBase
    {

        private readonly ILogger<AgentRequestController> _logger;

        public AgentRequestController(ILogger<AgentRequestController> logger)
        {
            _logger = logger;
        }

        [HttpDelete("{poolId}/{requestId}")]
        public void DeleteAgentRequest(int poolId, long requestId, string lockToken, string result = null)
        {
        }

        [HttpGet("{poolId}/{requestId}")]
        public TaskAgentJobRequest GetAgentRequest(int poolId, long requestId, string includeStatus = null)
        {
            return new TaskAgentJobRequest() {PlanId = Guid.NewGuid()};
        }

        [HttpPatch("{poolId}/{requestId}")]
        public TaskAgentJobRequest UpdateAgentRequest(int poolId, long requestId, [FromBody] TaskAgentJobRequest patch)
        {
            // var old = GetAgentRequest(poolId, requestId);
            // patch.ApplyTo(old);
            patch.LockedUntil = DateTime.UtcNow.AddDays(1);
            return patch;
        }
    }
}
