using System;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace Runner.Server.Azure.Devops {
    public class VersionWrapper : IString {
        public Version Version { get; set; }
        
        public String GetString() {
            return Version.ToString();
        }
    }
}