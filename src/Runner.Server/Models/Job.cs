using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Runner.Server.Controllers.MessageController;

namespace Runner.Server.Models {
    public class Job {
        public Job() {
            CancelRequest = new CancellationTokenSource();
            Outputs = new List<JobOutput>();
        }
        [NotMapped]
        [IgnoreDataMember]
        public Action CleanUp { get; set; }
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
        public string WorkflowIdentifier { get; set; }
        public string Matrix { get; set; }
        [NotMapped]
        [IgnoreDataMember]
        private ArrayContextData _matrix;

        [NotMapped]
        [IgnoreDataMember]
        public ArrayContextData MatrixContextData { get {
            if(_matrix != null) {
                return _matrix;
            }
            var rawmatrix = string.IsNullOrEmpty(Matrix) ? null : JToken.Parse(Matrix).ToPipelineContextData();
            if(rawmatrix is ArrayContextData amatrix) {
                _matrix = amatrix;
            } else {
                amatrix = new ArrayContextData();
                for(int i = 0, depth = WorkflowIdentifier?.Count(c => c == '/') ?? 0; i < depth; i++) {
                    amatrix.Add(null);
                }
                amatrix.Add(rawmatrix);
                _matrix = amatrix;
            }
            return _matrix;
        }}
        [NotMapped]
        [IgnoreDataMember]
        public TemplateToken MatrixToken { get {
            return MatrixContextData.ToTemplateToken();
        }}

        public string workflowname { get; set; }
        public long runid { get; set; }
        [IgnoreDataMember]
        public CancellationTokenSource CancelRequest { get; }
        [IgnoreDataMember]
        public bool Cancelled { get; internal set; }

        public double TimeoutMinutes {get;set;}
        public double CancelTimeoutMinutes {get;set;}
        public bool ContinueOnError {get;set;}
        public List<string> errors;

        public List<JobOutput> Outputs {get;set;}
        public TaskResult? Result {get;set;}

        [IgnoreDataMember]
        public List<TimelineRecord> TimeLine {get;set;}

        [NotMapped]
        public long Attempt { get => WorkflowRunAttempt?.Attempt ?? 1; }
    }
}