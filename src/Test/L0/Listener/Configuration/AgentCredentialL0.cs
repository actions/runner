using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public class TestAgentCredential : CredentialProvider
    {
        public TestAgentCredential(): base("TEST") {}
        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace("PersonalAccessToken");
            trace.Info("GetVssCredentials()");

            VssBasicCredential loginCred = new VssBasicCredential("test", "password");
            VssCredentials creds = new VssClientCredentials(loginCred);
            trace.Verbose("cred created");

            return creds;
        }        
        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool isUnattended)
        {
        }
    }
}