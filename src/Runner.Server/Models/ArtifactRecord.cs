namespace Runner.Server.Models {
    public class ArtifactRecord {
        public int Id { get; set; }
        public ArtifactFileContainer FileContainer { get; set; }
        public string FileName {get;set;}
        public string StoreName {get;set;}
        public bool GZip {get;set;}
    }
}