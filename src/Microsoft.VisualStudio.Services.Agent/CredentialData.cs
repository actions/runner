using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class CredentialData
    {
        public string Scheme { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}