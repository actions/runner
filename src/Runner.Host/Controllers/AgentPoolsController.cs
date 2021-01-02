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
    public class AgentPoolsController : ControllerBase
    {

        private readonly ILogger<AgentPoolsController> _logger;

        private List<TaskAgentPool> pool = new List<TaskAgentPool> {
            new TaskAgentPool("Agents") {
                Id = 1,
                IsHosted = false,
                IsInternal = true
            }
        };

        public AgentPoolsController(ILogger<AgentPoolsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public VssJsonCollectionWrapper<List<TaskAgentPool>> Get(string poolName = "", string properties = "", string poolType = "")
        {
            return new VssJsonCollectionWrapper<List<TaskAgentPool>> (pool);
        }
    }
}
