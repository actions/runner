using Microsoft.VisualStudio.Services.Agent.Configuration;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class TestAgentCredential : CredentialProvider
    {
        public TestAgentCredential(): base("TEST") {}
        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            TraceSource trace = context.GetTrace("PersonalAccessToken");
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