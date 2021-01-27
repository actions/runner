using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Runner.Host.Models;

namespace Runner.Host.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("runner/host/_apis/v1/[controller]")]
    public class AgentSessionController : VssControllerBase
    {

        private readonly ILogger<AgentSessionController> _logger;
        private IMemoryCache _cache;

        private SqLiteDb _context;
    

        public AgentSessionController(ILogger<AgentSessionController> logger, IMemoryCache cache, SqLiteDb context)
        {
            _logger = logger;
            _cache = cache;
            _context = context;
        }

        [HttpPost("{poolId}")]
        public async Task<IActionResult> Create(int poolId)
        {
            var session = await FromBody<TaskAgentSession>();
            session.SessionId = Guid.NewGuid();
            session.UseFipsEncryption = true;
            var aes = Aes.Create();
            Agent agent = Agent.GetAgent(_cache, _context, poolId, session.Agent.Id);
            if(agent == null) {
                return NotFound();
            }
            Session _session = _cache.Set(session.SessionId, new Session() {
                TaskAgentSession = session,
                Agent = agent,
                Key = aes
                
            });
            session.EncryptionKey = new TaskAgentSessionKey() {
                Encrypted = true,
                Value = _session.Agent.PublicKey.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256)
            };
            
            return await Ok(session);
        }

        [HttpDelete("{poolId}/{sessionId}")]
        public void Delete(int poolId, Guid sessionId)
        {
            TaskAgentSession session;
            if(_cache.TryGetValue(sessionId, out session)) {
                _cache.Remove(sessionId);
            }
        }
    }
}
