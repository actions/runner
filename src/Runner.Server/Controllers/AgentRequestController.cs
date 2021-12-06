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

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Agent")]
    public class AgentRequestController : VssControllerBase
    {
        private IMemoryCache _cache;

        public AgentRequestController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpDelete("{poolId}/{requestId}")]
        public void DeleteAgentRequest(int poolId, long requestId, string lockToken, string result = null)
        {
        }

        [HttpGet("{poolId}/{requestId}")]
        public Task<FileStreamResult> GetAgentRequest(int poolId, long requestId, string includeStatus = null)
        {
            // Set Result here to avoid the runner ignoring a request, actually that doesn't work either there is a race condition in the runner
            return Ok(new TaskAgentJobRequest() {PlanId = Guid.NewGuid(), Result = TaskResult.Succeeded});
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
