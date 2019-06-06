using System;
using System.Collections.Generic;

namespace GitHub.Services.Licensing
{
    public class ServiceRight : IServiceRight
    {
        public Dictionary<string, object> Attributes { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        public string Name { get; set; }

        public VisualStudioOnlineServiceLevel ServiceLevel { get; set; }

        public Version Version { get; set; }

    }
}
