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
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Agent")]
    public class AgentSessionController : VssControllerBase
    {

        private IMemoryCache _cache;

        private SqLiteDb _context;
    

        public AgentSessionController(IMemoryCache cache, SqLiteDb context)
        {
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
            await _context.Entry(agent).Reference(a => a.TaskAgent).TargetEntry.Collection(a => a.Labels).LoadAsync();
            Session _session = _cache.Set(session.SessionId, new Session() {
                TaskAgentSession = session,
                Agent = agent,
                Key = aes
                
            });
            // session.EncryptionKey = new TaskAgentSessionKey() {
            //     Encrypted = false,
            //     Value = aes.Key
            // };
            session.EncryptionKey = new TaskAgentSessionKey() {
                Encrypted = true,
                Value = _session.Agent.PublicKey.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256)
            };

            MessageController.sessions.AddOrUpdate(_session, s => {
                if(s.Timer == null) {
                    s.Timer = new System.Timers.Timer();
                }
                s.Timer.AutoReset = false;
                s.Timer.Interval = 60000;
                s.Timer.Elapsed += (a,b) => {
                    Session s2;
                    MessageController.sessions.TryRemove(_session, out s2);
                };
                s.Timer.Start();
                return s;
            } , (s, v) => {
                s.Timer.Stop();
                s.Timer.Start();
                return v;
            });
            
            return await Ok(session);
        }

        [HttpDelete("{poolId}/{sessionId}")]
        public void Delete(int poolId, Guid sessionId)
        {
            Session session;
            if(_cache.TryGetValue(sessionId, out session)) {
                session.DropMessage?.Invoke();
                session.DropMessage = null;
                _cache.Remove(sessionId);
                Session s2;
                MessageController.sessions.TryRemove(session, out s2);
            }
        }
    }
}
