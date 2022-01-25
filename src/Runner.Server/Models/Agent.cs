using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using GitHub.DistributedTask.WebApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Runner.Server.Models
{
    public class Agent {

        [IgnoreDataMember]
        private int? _id;
        public int Id { get => TaskAgent?.Id ?? _id ?? 0; set {
            _id = value;
            if(TaskAgent != null) {
                TaskAgent.Id = value;
            }
        } }
        public Pool Pool {get;set;}

        public TaskAgent TaskAgent {get;set;}

        [IgnoreDataMember]
        public RSA PublicKey {
            get => RSA.Create(new RSAParameters() { Exponent = Exponent, Modulus = Modulus });
        }

        public Guid ClientId { get; set; }
        public byte[] Exponent { get; set; }
        public byte[] Modulus { get; set; }

        public static Agent GetAgent(IMemoryCache cache, SqLiteDb db, int poolId, int id)
        {
            var ret = db.Agents.Include(a => a.TaskAgent).Include(a => a.TaskAgent.Labels).Include(a => a.Pool).Where(a => a.Id == id).FirstOrDefault();
            return ret;
        }

        public static Agent CreateAgent(IMemoryCache cache, SqLiteDb db, int poolId, TaskAgent agent)
        {
            var _agent = new Agent();
            _agent.Exponent = agent.Authorization.PublicKey.Exponent;
            _agent.Modulus = agent.Authorization.PublicKey.Modulus;
            _agent.ClientId = agent.Authorization.ClientId;
            _agent.TaskAgent = agent;
            _agent.Pool = Pool.GetPoolById(cache, db, poolId);
            db.Agents.Add(_agent);
            return _agent;
        }
    }
}