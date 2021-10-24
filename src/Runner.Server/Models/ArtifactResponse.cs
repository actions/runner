namespace Runner.Server.Models {
    public class ArtifactResponse {
        public string containerId {get;set;}
        public int size {get;set;}
        public string signedContent {get;set;}
        public string fileContainerResourceUrl {get;set;}
        public string type {get;set;}
        public string name {get;set;}
        public string url {get;set;}
    }
}