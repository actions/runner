using System;
using System.Security.Cryptography;
using GitHub.DistributedTask.WebApi;
using Microsoft.Extensions.Caching.Memory;

namespace Runner.Host.Models
{
    public class Agent {
        public static readonly String CachePrefix = "Agent_";
        public Pool Pool {get;set;}

        public TaskAgent TaskAgent {get;set;}

        public RSA PublicKey {get;set;}

        public static Agent GetAgent(IMemoryCache cache, int poolId, long id)
        {
            return cache.Get<Agent>($"{CachePrefix}{poolId}_{id}");
        }

        public static Agent CreateAgent(IMemoryCache cache, int poolId, TaskAgent agent, int? agentId = null)
        {
            var pool = Pool.GetPoolById(cache, poolId);
            var id = agentId ?? (pool.Agents.Count > 0 ? pool.Agents[pool.Agents.Count - 1].TaskAgent.Id + 1 : 1);
            agent.Id = id;
            return cache.Set($"{CachePrefix}{poolId}_{id}", new Agent() { TaskAgent = agent, Pool = pool });
        }
    }
}