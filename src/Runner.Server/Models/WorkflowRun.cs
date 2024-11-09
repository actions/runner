using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using GitHub.DistributedTask.WebApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Runner.Server.Models
{
    public class WorkflowRun {

        public long Id { get; set; }
        [IgnoreDataMember]
        public Workflow Workflow { get; set; }
        public String FileName { get; set; }
        public String DisplayName { get; set; }
        [IgnoreDataMember]
        public List<WorkflowRunAttempt> Attempts { get; set; }
        [NotMapped]
        public String EventName { get; set; }
        [NotMapped]
        public String Ref { get; set; }
        [NotMapped]
        public String Sha { get; set; }
        [NotMapped]
        public TaskResult? Result { get; set; }
        [NotMapped]
        public Status Status { get; set; }
        [NotMapped]
        public String Owner { get; set; }
        [NotMapped]
        public String Repo { get; set; }
    }
}