using System.Collections.Generic;

using Microsoft.VisualStudio.Services.Agent.Configuration;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class TestAgentCredential : CredentialProvider
    {
        public TestAgentCredential(): base("TEST") {}
        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool isUnattended)
        {
        }
    }
}