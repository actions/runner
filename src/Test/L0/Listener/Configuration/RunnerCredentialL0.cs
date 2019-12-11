using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.OAuth;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class TestRunnerCredential : CredentialProvider
    {
        public TestRunnerCredential() : base("TEST") { }
        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace("OuthAccessToken");
            trace.Info("GetVssCredentials()");
            
            var loginCred = new VssOAuthAccessTokenCredential("sometoken");
            VssCredentials creds = new VssCredentials(loginCred);
            trace.Verbose("cred created");

            return creds;
        }
        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
        }
    }
}
