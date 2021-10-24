using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Threading;
using GitHub.DistributedTask.WebApi;
using static Runner.Server.Controllers.MessageController;

namespace Runner.Server.Models {
    public class Job {
        public Job() {
            CancelRequest = new CancellationTokenSource();
            Outputs = new List<JobOutput>();
        }
        [IgnoreDataMember]
        public WorkflowRunAttempt WorkflowRunAttempt { get; set; }
        public Guid JobId { get; set; }
        public long RequestId { get; set; }
        public Guid TimeLineId { get; set; }
        public Guid SessionId { get; set; }
        [IgnoreDataMember]
        public MessageFactory message;

        public string repo { get; set; }
        public string name { get; set; }
        public string workflowname { get; set; }
        public long runid { get; set; }
        public CancellationTokenSource CancelRequest { get; }
        public bool Cancelled { get; internal set; }

        public double TimeoutMinutes {get;set;}
        public double CancelTimeoutMinutes {get;set;}
        public bool ContinueOnError {get;set;}
        public List<string> errors;
        [NotMapped]
        public JobCompletedEvent JobCompletedEvent {get;set;}

        public List<JobOutput> Outputs {get;set;}
        public TaskResult? Result {get;set;}

        public List<TimelineRecord> TimeLine {get;set;}

    }
}