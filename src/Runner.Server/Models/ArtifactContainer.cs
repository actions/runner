using System.Collections.Generic;

namespace Runner.Server.Models {
    public class ArtifactContainer {
        public int Id { get; set; }
        public WorkflowRunAttempt Attempt { get; set; }
        public List<ArtifactFileContainer> FileContainer {get;set;}
    }
}