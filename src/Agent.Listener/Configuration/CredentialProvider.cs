using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public interface ICredentialProvider
    {
        CredentialData CredentialData { get; set; }
        VssCredentials GetVssCredentials(IHostContext context);
        void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied);
    }

    public abstract class CredentialProvider : ICredentialProvider
    {
        public CredentialProvider(string scheme)
        {
            CredentialData = new CredentialData();
            CredentialData.Scheme = scheme;
            CredentialData.Data = new Dictionary<string, string>();
        }

        public CredentialData CredentialData { get; set; }

        public abstract VssCredentials GetVssCredentials(IHostContext context);
        public abstract void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied);
    }

    public sealed class PersonalAccessToken : CredentialProvider
    {
        public PersonalAccessToken(): base("PAT") {}
        
        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace("PersonalAccessToken");
            trace.Info("GetVssCredentials()");

            if (CredentialData == null || !CredentialData.Data.ContainsKey("token"))
            {
                throw new InvalidOperationException("Must call ReadCredential first.");
            }

            string token = CredentialData.Data["token"];
            trace.Info("token retrieved: {0} chars", token.Length);

            // PAT uses a basic credential
            VssBasicCredential loginCred = new VssBasicCredential("VstsAgent", token);
            VssCredentials creds = new VssClientCredentials(loginCred);
            trace.Verbose("cred created");

            return creds;
        }

        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied)
        {
            Tracing trace = context.GetTrace("PersonalAccessToken");
            trace.Info("ReadCredentials()");

            var wizard = context.GetService<IConsoleWizard>();
            trace.Verbose("reading token");
            string tokenVal = wizard.ReadValue("token", 
                                            "PersonalAccessToken", 
                                            true, 
                                            String.Empty, 
                                            // can do better
                                            Validators.NonEmptyValidator,
                                            args, 
                                            enforceSupplied);
            CredentialData.Data["token"] = tokenVal;
        }        
    }

    public sealed class AlternateCredential : CredentialProvider
    {
        public AlternateCredential(): base("ALT") {}

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace("PersonalAccessToken");
            trace.Info("GetVssCredentials()");

            if (CredentialData == null || !CredentialData.Data.ContainsKey("token"))
            {
                throw new InvalidOperationException("Must call ReadCredential first.");
            }

            string username = CredentialData.Data["Username"];
            trace.Info("username retrieved: {0} chars", username.Length);

            string password = CredentialData.Data["Password"];
            trace.Info("password retrieved: {0} chars", password.Length);

            // PAT uses a basic credential
            VssBasicCredential loginCred = new VssBasicCredential(username, password);
            VssCredentials creds = new VssClientCredentials(loginCred);
            trace.Verbose("cred created");

            return creds;
        }

        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied)
        {
            var wizard = context.GetService<IConsoleWizard>();
            CredentialData.Data["Username"] = wizard.ReadValue("username", 
                                            "Username", 
                                            false,
                                            String.Empty,
                                            // can do better
                                            Validators.NonEmptyValidator, 
                                            args, 
                                            enforceSupplied);

            CredentialData.Data["Password"] = wizard.ReadValue("password", 
                                            "Password", 
                                            true,
                                            String.Empty,
                                            // can do better
                                            Validators.NonEmptyValidator,
                                            args, 
                                            enforceSupplied);            
        }        
    }   
}