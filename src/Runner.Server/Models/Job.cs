using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using static Runner.Server.Controllers.MessageController;

namespace Runner.Server.Models {
    public class Job {
        public Job() {
            CancelRequest = new CancellationTokenSource();
            Outputs = new List<JobOutput>();
        }
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
        private TemplateToken _matrix;
        [NotMapped]
        public TemplateToken MatrixToken { get {
            if(_matrix != null) {
                return _matrix;
            } else if(!string.IsNullOrEmpty(Matrix)) {
                using (var stringReader = new StringReader(Matrix)) {
                    var templateContext = new TemplateContext(){
                        CancellationToken = CancellationToken.None,
                        Errors = new TemplateValidationErrors(10, 500),
                        Memory = new TemplateMemory(
                            maxDepth: 100,
                            maxEvents: 1000000,
                            maxBytes: 10 * 1024 * 1024),
                        Schema = PipelineTemplateSchemaFactory.GetSchema()
                    };
                    var yamlObjectReader = new YamlObjectReader(0, stringReader);
                    _matrix = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
                    return _matrix;
                }
            }
            return new NullToken(null, null, null);
        }}

        public string workflowname { get; set; }
        public long runid { get; set; }
        public CancellationTokenSource CancelRequest { get; }
        public bool Cancelled { get; internal set; }

        public double TimeoutMinutes {get;set;}
        public double CancelTimeoutMinutes {get;set;}
        public bool ContinueOnError {get;set;}
        public List<string> errors;

        public List<JobOutput> Outputs {get;set;}
        public TaskResult? Result {get;set;}

        public List<TimelineRecord> TimeLine {get;set;}

    }
}