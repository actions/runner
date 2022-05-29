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
        [IgnoreDataMember]
        public WorkflowRun WorkflowRun { get; set; }
        public int Attempt { get; set; }
        public int ArtifactsMinAttempt { get; set; }
        public string EventName { get; set; }
        public string EventPayload { get; set; }
        public string Workflow { get; set; }
        [IgnoreDataMember]
        public List<Job> Jobs { get; set; }
        [IgnoreDataMember]
        public List<ArtifactContainer> Artifacts { get; set; }
        public Guid TimeLineId { get; set; }
        public string Ref { get; set; }
        public string Sha { get; set; }
        public string StatusCheckSha { get; set; }
        public TaskResult? Result { get; set; }
    }
}