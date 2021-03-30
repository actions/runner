using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Runner.Host.Models;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class AgentPoolsController : VssControllerBase
    {

        private readonly ILogger<AgentPoolsController> _logger;
        private IMemoryCache _cache;

        private List<Pool> pools;

        private SqLiteDb db;

        public AgentPoolsController(ILogger<AgentPoolsController> logger, IMemoryCache cache, SqLiteDb db)
        {
            this.db = db;
            _logger = logger;
            _cache = cache;
            if(!db.Pools.Any() && !_cache.TryGetValue(Pool.CachePools, out pools)) {
                pools = new List<Pool> {
                    new Pool() { 
                        TaskAgentPool = new TaskAgentPool("Agents") {
                            Id = 1,
                            IsHosted = false,
                            IsInternal = true
                        }
                    }
                };
                _cache.Set(Pool.CachePrefix + 1, pools[0]);
                _cache.Set(Pool.CachePools, pools);
            }
        }

        [HttpGet]
        public Task<FileStreamResult> Get(string poolName = "", string properties = "", string poolType = "")
        {
            return Ok(new VssJsonCollectionWrapper<List<TaskAgentPool>> ((from pool in pools ?? db.Pools.Include(a => a.TaskAgentPool).AsEnumerable() select pool.TaskAgentPool).ToList()));
        }
    }
}
