using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AgentPoolsController : VssControllerBase
    {

        private IMemoryCache _cache;

        private SqLiteDb db;

        public AgentPoolsController(IMemoryCache cache, SqLiteDb db, IConfiguration conf) : base(conf)
        {
            this.db = db;
            _cache = cache;
            if(!db.Pools.Any()) {
                db.Pools.Add(new Pool() { 
                    TaskAgentPool = new TaskAgentPool("Agents") {
                        IsHosted = false,
                        IsInternal = true
                    }
                });
                db.SaveChanges();
            }
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public Task<FileStreamResult> Get(string poolName = "", string properties = "", string poolType = "")
        {
            return Ok((from pool in db.Pools.Include(a => a.TaskAgentPool).AsEnumerable() select pool.TaskAgentPool).ToList());
        }
    }
}
