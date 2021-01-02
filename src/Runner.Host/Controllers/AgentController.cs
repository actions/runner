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
    public class AgentController : ControllerBase
    {

        private readonly ILogger<AgentController> _logger;

        private List<TaskAgent> pool = new List<TaskAgent> ();

        public AgentController(ILogger<AgentController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{poolId}")]
        public TaskAgent Post(int poolId, [FromBody] TaskAgent agent) {
            pool.Add(agent);
            agent.Authorization = new TaskAgentAuthorization() {
                AuthorizationUrl = new Uri("https://localhost:5001/test/auth/v1/"),
                ClientId = Guid.NewGuid(),
                PublicKey = new TaskAgentPublicKey()
            };
            return agent;
        }

        [HttpGet("{poolId}/{agentId}")]
        public TaskAgent Get(int poolId, long agentId)
        {
            return new TaskAgent("agent");
        }

        [HttpGet("{poolId}")]
        public VssJsonCollectionWrapper<List<TaskAgent>> Get(int poolId)
        {
            return new VssJsonCollectionWrapper<List<TaskAgent>> (
                pool
            );
        }
    }
}
