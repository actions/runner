using System.Collections.Generic;

namespace Runner.Server.Models {
    public class ArtifactFileContainer {
        public int Id { get; set; }
        public ArtifactContainer Container { get; set; }
        public string Name {get;set;}
        public List<ArtifactRecord> Files {get;set;}
        public long? Size { get; internal set; }
    }
}