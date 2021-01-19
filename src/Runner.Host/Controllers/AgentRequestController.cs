using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("runner/host/_apis/v1/[controller]")]
    public class AgentRequestController : VssControllerBase
    {
        private readonly ILogger<AgentRequestController> _logger;

        private IMemoryCache _cache;

        public AgentRequestController(ILogger<AgentRequestController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
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
        public async Task<IActionResult> UpdateAgentRequest(int poolId, long requestId)
        {
            var patch = await FromBody<TaskAgentJobRequest>();
            patch.LockedUntil = DateTime.UtcNow.AddDays(1);
            return await Ok(patch);
        }
    }
}
