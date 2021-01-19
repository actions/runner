using System;
using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;
using Microsoft.Extensions.Caching.Memory;

namespace Runner.Host.Models
{
    public class Pool {
        public Pool() {
            Agents = new List<Agent>();
        }
        public static readonly String CachePools = "AgentPools";
        public static readonly String CachePrefix = "AgentPool_";
        public TaskAgentPool TaskAgentPool {get;set;}
        public List<Agent> Agents {get;set;}
        public static Pool GetPoolById(IMemoryCache cache, int id) {
            return cache.Get<Pool>(CachePrefix + id);
        }
    }
}