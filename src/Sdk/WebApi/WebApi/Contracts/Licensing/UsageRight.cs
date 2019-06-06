using System;
using System.Collections.Generic;

namespace GitHub.Services.Licensing
{
    public class UsageRight : IUsageRight
    {
        public Dictionary<string, object> Attributes { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        public string Name { get; set; }

        public Version Version { get; set; }
    }
}
