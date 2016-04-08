using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public sealed class NTLMCredential : CredentialProvider
    {
        public NTLMCredential() : base("NTLM") { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace(nameof(NTLMCredential));
            trace.Info(nameof(GetVssCredentials));

            if (CredentialData == null || !CredentialData.Data.ContainsKey("Username")
                || !CredentialData.Data.ContainsKey("Password") || !CredentialData.Data.ContainsKey("Url"))
            {
                throw new InvalidOperationException("Must call ReadCredential first.");
            }

            string username = CredentialData.Data["Username"];
            trace.Info($"username retrieved: {username.Length} chars");

            string password = CredentialData.Data["Password"];
            trace.Info($"password retrieved: {password.Length} chars");

            //create NTLM credentials
            var credential = new NetworkCredential(username, password);
            var myCache = new CredentialCache();            
            myCache.Add(new Uri(CredentialData.Data["Url"]), "NTLM", credential);
            VssCredentials creds = new VssClientCredentials(new WindowsCredential(myCache));

            trace.Verbose("cred created");

            return creds;
        }

        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied)
        {
            var wizard = context.GetService<IConsoleWizard>();
            CredentialData.Data["Username"] = wizard.ReadValue(CliArgs.UserName,
                                            StringUtil.Loc("NTLMUsername"),
                                            false,
                                            String.Empty,
                                            //TODO: use Validators.NTAccountValidator when it works on Linux
                                            Validators.NonEmptyValidator,
                                            args,
                                            enforceSupplied);

            CredentialData.Data["Password"] = wizard.ReadValue(CliArgs.Password,
                                            StringUtil.Loc("NTLMPassword"),
                                            true,
                                            String.Empty,
                                            Validators.NonEmptyValidator,
                                            args,
                                            enforceSupplied);

            CredentialData.Data["Url"] = args[CliArgs.Url];
        }
    }
}
