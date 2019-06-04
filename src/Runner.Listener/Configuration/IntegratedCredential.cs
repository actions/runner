using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;

namespace GitHub.Runner.Listener.Configuration
{
    public sealed class IntegratedCredential : CredentialProvider
    {
        public IntegratedCredential() : base(Constants.Configuration.Integrated) { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(IntegratedCredential));
            trace.Info(nameof(GetVssCredentials));

            // Create instance of VssConnection using default Windows credentials (NTLM)
            VssCredentials creds = new VssCredentials(true);

            trace.Verbose("cred created");

            return creds;
        }

        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
            //Integrated credentials do not require any configuration parameters
        }
    }
}
