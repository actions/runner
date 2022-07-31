using System;
using System.Collections.Generic;
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
        public IList<Capability> Capabilities { get; set; } = new List<Capability>();

        public static Agent GetAgent(IMemoryCache cache, SqLiteDb db, int poolId, int id)
        {
            var ret = db.Agents.Include(a => a.TaskAgent).Include(a => a.TaskAgent.Labels).Include(a => a.Capabilities).Include(a => a.Pool).Where(a => a.Id == id).FirstOrDefault();
            if(ret?.TaskAgent != null) {
                foreach(var cap in ret.Capabilities) {
                    (cap.System ? ret.TaskAgent.SystemCapabilities : ret.TaskAgent.UserCapabilities).Add(cap.Name, cap.Value);
                }
            }
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
            if(agent.SystemCapabilities?.Any() ?? false) {
                foreach(var kv in agent.SystemCapabilities) {
                    _agent.Capabilities.Add(new Capability() { Name = kv.Key, Value = kv.Value, System = true });
                }
            }
            if(agent.UserCapabilities?.Any() ?? false) {
                foreach(var kv in agent.UserCapabilities) {
                    _agent.Capabilities.Add(new Capability() { Name = kv.Key, Value = kv.Value });
                }
            }
            db.Agents.Add(_agent);
            return _agent;
        }
    }
}