using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
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

        public override Task EnsureCredential(IHostContext context, CommandSettings command, string serverUrl, CancellationToken token)
        {
            return Task.CompletedTask;
            //Integrated credentials do not require any configuration parameters
        }
    }
}
