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
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentManagementRead")]
    public class AgentController : VssControllerBase
    {
        private IMemoryCache _cache;

        private SqLiteDb _context;

        public AgentController(IMemoryCache cache, SqLiteDb context, IConfiguration conf) : base(conf)
        {
            _cache = cache;
            _context = context;
        }

        private static object lok = new object();

        [HttpPost("{poolId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentManagement")]
        public async Task<IActionResult> Post(int poolId) {
            TaskAgent agent = await FromBody<TaskAgent>();
            lock(lok) {
                // Without a lock we get rsa message exchange problems, decryption error of rsa encrypted session aes key
                agent.Authorization.AuthorizationUrl = new Uri(new Uri(ServerUrl), "_apis/v1/auth/");
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
        public IActionResult Delete(int poolId, int agentId)
        {
            var agent = Agent.GetAgent(_cache, _context, poolId, agentId);
            lock(lok) {
                _context.Agents.Remove(agent);
                _context.SaveChanges();
            }
            return NoContent();
        }

        [HttpPut("{poolId}/{agentId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentManagement")]
        public async Task<ActionResult> Replace(int poolId, int agentId)
        {
            TaskAgent tagent = await FromBody<TaskAgent>();
            lock(lok) {
                var agent = Agent.GetAgent(_cache, _context, poolId, agentId);
                agent.TaskAgent.Authorization = new TaskAgentAuthorization() { ClientId = agent.ClientId, PublicKey = new TaskAgentPublicKey(agent.Exponent, agent.Modulus), AuthorizationUrl = new Uri(new Uri(ServerUrl), "_apis/v1/auth/") };
                agent.TaskAgent.Labels.Clear();
                foreach(var l in tagent.Labels) {
                    agent.TaskAgent.Labels.Add(l);
                }
                agent.TaskAgent.Name = tagent.Name;
                agent.TaskAgent.Ephemeral = tagent.Ephemeral;
                agent.TaskAgent.OSDescription = tagent.OSDescription;
                if(tagent.Authorization != null) {
                    if(tagent.Authorization.PublicKey?.Exponent != null && tagent.Authorization.PublicKey?.Modulus != null) {
                        agent.TaskAgent.Authorization.PublicKey.Exponent = tagent.Authorization.PublicKey.Exponent;
                        agent.TaskAgent.Authorization.PublicKey.Modulus = tagent.Authorization.PublicKey.Modulus;
                    }
                    if(tagent.Authorization.ClientId != Guid.Empty) {
                        agent.TaskAgent.Authorization.ClientId = tagent.Authorization.ClientId;
                    }
                    agent.Exponent = agent.TaskAgent.Authorization.PublicKey.Exponent;
                    agent.Modulus = agent.TaskAgent.Authorization.PublicKey.Modulus;
                    agent.ClientId = agent.TaskAgent.Authorization.ClientId;
                }
                agent.TaskAgent.ProvisioningState = tagent.ProvisioningState;
                agent.TaskAgent.Enabled = tagent.Enabled;
                _context.SaveChanges();
                tagent = agent.TaskAgent;
            }
            return await Ok(tagent);
        }

        [HttpGet("{poolId}")]
        public async Task<ActionResult> Get(int poolId, [FromQuery] string agentName)
        {
            return await Ok(new VssJsonCollectionWrapper<IEnumerable<TaskAgent>>(from agent in _context.Agents where (poolId == 0 || poolId == -1 || agent.Pool.Id == poolId) && (string.IsNullOrEmpty(agentName) || agent.TaskAgent.Name == agentName) select agent.TaskAgent));
        }
    }
}
