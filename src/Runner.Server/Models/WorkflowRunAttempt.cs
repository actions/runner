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
    public class WorkflowRunAttempt {

        public int Id { get; set; }
        public WorkflowRun WorkflowRun { get; set; }
        public int Attempt { get; set; }
        public string EventName { get; set; }
        public string EventPayload { get; set; }
        public string Workflow { get; set; }
        public List<Job> Jobs { get; set; }
        public List<ArtifactContainer> Artifacts { get; set; }
    }
}