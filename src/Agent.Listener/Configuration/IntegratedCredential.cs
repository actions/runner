using Microsoft.VisualStudio.Services.Common;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public sealed class IntegratedCredential : CredentialProvider
    {
        public IntegratedCredential() : base(Constants.Configuration.Integrated) { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace(nameof(NegotiateCredential));
            trace.Info(nameof(GetVssCredentials));

            // Create instance of VssConnection using default Windows credentials (NTLM)
            VssCredentials creds = new VssCredentials(true);

            trace.Verbose("cred created");

            return creds;
        }

        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied)
        {
            //Integrated credentials do not require any configuration parameters
        }
    }
}
