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
    public class Workflow {

        public int Id { get; set; }
        public Repository Repository { get; set; }
        public String FileName { get; set; }
        public String DisplayName { get; set; }
        public List<WorkflowRun> Runs { get; set; }
    }
}