using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using Microsoft.Extensions.Caching.Memory;

namespace Runner.Server.Models
{
    public class Pool {
        public Pool() {
            Agents = new List<Agent>();
        }

        [IgnoreDataMember]
        private int? _id;
        public int Id { get => TaskAgentPool?.Id ?? _id ?? 0; set {
            _id = value;
            if(TaskAgentPool != null) {
                TaskAgentPool.Id = value;
            }
        } }
        public static readonly String CachePools = "AgentPools";
        public static readonly String CachePrefix = "AgentPool_";
        public TaskAgentPool TaskAgentPool {get;set;}
        public List<Agent> Agents {get;set;}
        public static Pool GetPoolById(IMemoryCache cache, SqLiteDb db, int id) {
            return cache.Get<Pool>(CachePrefix + id) ?? db.Pools.Find(id);
        }
    }
}