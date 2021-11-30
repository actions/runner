using System;

namespace Runner.Server.Models {
    public class CacheRecord {
        public int Id {get;set;}
        public string Key {get;set;}
        public string Repo {get;set;}
        public string Ref {get;set;}
        public string Storage {get;set;}
        public DateTime LastUpdated {get;set;}
    }
}