using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class LogfilesController : VssControllerBase
    {        
        private SqLiteDb _context;

        public LogfilesController(SqLiteDb context, IConfiguration conf) : base(conf) 
        {
            _context = context;
        }
        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
        public async Task<IActionResult> CreateLog(Guid scopeIdentifier, string hubName, Guid planId)
        {
            var log = await FromBody<TaskLog>();
            _context.Logs.Add(new SqLiteDb.LogStorage() { Ref = log });
            await _context.SaveChangesAsync();
            return await Ok(log);
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/{logId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
        public async Task AppendLogContent(Guid scopeIdentifier, string hubName, Guid planId, int logId)
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var log = (from l in _context.Logs where l.Ref.Id == logId select l).First();
                log.Content += await reader.ReadToEndAsync();
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet("{logId}")]
        public string GetLog(int logId)
        {
            return (from l in _context.Logs where l.Ref.Id == logId select l).First().Content;
        }
    }
}
