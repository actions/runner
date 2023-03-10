using System;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace Runner.Server.Azure.Devops {
    public class DateTimeWrapper : IString {
        public DateTimeOffset DateTime { get; set; }
        
        public String GetString() {
            // 2022-11-20 15:38:45+00:00
            return DateTime.ToString("yyyy-MM-dd HH:mm:ssK");
        }
    }
}