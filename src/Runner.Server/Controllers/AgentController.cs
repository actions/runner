using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AgentController : VssControllerBase
    {
        private IMemoryCache _cache;

        private SqLiteDb _context;

        public AgentController(IMemoryCache cache, SqLiteDb context, IConfiguration configuration = null)
        {
            _cache = cache;
            _context = context;
            ReadConfig(configuration);
        }

        private static object lok = new object();

        [HttpPost("{poolId}")]
        [HttpPatch("{poolId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentManagement")]
        public async Task<IActionResult> Post(int poolId) {
            TaskAgent agent = await FromBody<TaskAgent>();
            lock(lok) {
                // Without a lock we get rsa message exchange problems, decryption error of rsa encrypted session aes key
                agent.Authorization.AuthorizationUrl = new Uri($"{ServerUrl}/_apis/v1/auth/");
                agent.Authorization.ClientId = Guid.NewGuid();
                Agent _agent = Agent.CreateAgent(_cache, _context, poolId, agent);
                _context.SaveChanges();
            }
            return await Ok(agent);
        }

        [HttpGet("{poolId}/{agentId}")]
        public async Task<ActionResult> Get(int poolId, int agentId)
        {
            return await Ok(Agent.GetAgent(_cache, _context, poolId, agentId).TaskAgent);
        }

        [HttpDelete("{poolId}/{agentId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentManagement")]
        public async Task<ActionResult> Delete(int poolId, int agentId)
        {
            var agent = Agent.GetAgent(_cache, _context, poolId, agentId);
            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
            _cache.Remove($"{Agent.CachePrefix}{poolId}_{agentId}");
            return NoContent();
        }

        [HttpGet("{poolId}")]
        public async Task<ActionResult> Get(int poolId, [FromQuery] string agentName)
        {
            var pool = Pool.GetPoolById(_cache, _context, poolId);
            if(pool == null) {
                return NotFound();
            }
            await _context.Entry(pool).Collection(p => p.Agents).LoadAsync();
            foreach (var item in pool.Agents)
            {
                await _context.Entry(item).Reference(p => p.TaskAgent).LoadAsync();
            }
            return await Ok(new VssJsonCollectionWrapper<List<TaskAgent>> (
                (from agent in pool.Agents ?? new List<Agent>() where agent != null && agent.TaskAgent.Name == agentName select agent.TaskAgent).ToList()
            ));
        }
    }
}
