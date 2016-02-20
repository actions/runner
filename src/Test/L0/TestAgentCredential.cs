using System.Collections.Generic;

using Microsoft.VisualStudio.Services.Agent.Configuration;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class TestAgentCredential : AgentCredential
    {
        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool isUnattended)
        {
        }
    }
}