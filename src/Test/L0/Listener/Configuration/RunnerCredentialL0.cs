using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Client;
using GitHub.Services.Common;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class TestRunnerCredential : CredentialProvider
    {
        public TestRunnerCredential(): base("TEST") {}
        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace("PersonalAccessToken");
            trace.Info("GetVssCredentials()");

            VssBasicCredential loginCred = new VssBasicCredential("test", "password");
            VssCredentials creds = new VssCredentials(loginCred);
            trace.Verbose("cred created");

            return creds;
        }        
        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
        }
    }
}
