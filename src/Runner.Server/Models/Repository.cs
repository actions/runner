using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Runner.Server.Models
{
    public class Repository {
        public Int64 Id { get; set; }
        public string Name { get; set; }
        public List<Workflow> Workflows { get; set; }
        // public List<Secret> Secrets { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public List<Pool> Pools { get; set; }
        public Owner Owner { get; set; }
    }
}