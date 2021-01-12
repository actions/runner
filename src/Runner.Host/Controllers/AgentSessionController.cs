using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Runner.Host.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("_apis/v1/[controller]")]
    public class AgentSessionController : ControllerBase
    {

        private readonly ILogger<AgentSessionController> _logger;

        public AgentSessionController(ILogger<AgentSessionController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{poolId}")]
        public TaskAgentSession Create(int poolId, [FromBody] TaskAgentSession session)
        {
            session.SessionId = Guid.NewGuid();
            return session;
        }

        [HttpDelete("{poolId}/{sessionId}")]
        public void Delete(int poolId, Guid sessionId)
        {

        }
    }
}
