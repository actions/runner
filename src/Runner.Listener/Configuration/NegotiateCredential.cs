﻿using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Client;
using GitHub.Services.Common;
using System;
using System.Net;

namespace GitHub.Runner.Listener.Configuration
{
    public sealed class NegotiateCredential : CredentialProvider
    {
        public NegotiateCredential() : base(Constants.Configuration.Negotiate) { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(NegotiateCredential));
            trace.Info(nameof(GetVssCredentials));
            ArgUtil.NotNull(CredentialData, nameof(CredentialData));

            // Get the user name from the credential data.
            string userName;
            if (!CredentialData.Data.TryGetValue(Constants.Runner.CommandLine.Args.UserName, out userName))
            {
                userName = null;
            }

            ArgUtil.NotNullOrEmpty(userName, nameof(userName));
            trace.Info("User name retrieved.");

            // Get the password from the credential data.
            string password;
            if (!CredentialData.Data.TryGetValue(Constants.Runner.CommandLine.Args.Password, out password))
            {
                password = null;
            }

            ArgUtil.NotNullOrEmpty(password, nameof(password));
            trace.Info("Password retrieved.");

            // Get the URL from the credential data.
            string url;
            if (!CredentialData.Data.TryGetValue(Constants.Runner.CommandLine.Args.Url, out url))
            {
                url = null;
            }

            ArgUtil.NotNullOrEmpty(url, nameof(url));
            trace.Info($"URL retrieved: {url}");

            // Create the Negotiate and NTLM credential object.
            var credential = new NetworkCredential(userName, password);
            var credentialCache = new CredentialCache();
            switch (Constants.Runner.Platform)
            {
                case Constants.OSPlatform.Linux:
                case Constants.OSPlatform.OSX:
                    credentialCache.Add(new Uri(url), "NTLM", credential);
                    break;
                case Constants.OSPlatform.Windows:
                    credentialCache.Add(new Uri(url), "Negotiate", credential);
                    break;
            }

            VssCredentials creds = new VssCredentials(new WindowsCredential(credentialCache), CredentialPromptType.DoNotPrompt);
            trace.Verbose("cred created");
            return creds;
        }

        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(PersonalAccessToken));
            trace.Info(nameof(EnsureCredential));
            ArgUtil.NotNull(command, nameof(command));
            ArgUtil.NotNullOrEmpty(serverUrl, nameof(serverUrl));
            //TODO: use Validators.NTAccountValidator when it works on Linux
            CredentialData.Data[Constants.Runner.CommandLine.Args.UserName] = command.GetUserName();
            CredentialData.Data[Constants.Runner.CommandLine.Args.Password] = command.GetPassword();
            CredentialData.Data[Constants.Runner.CommandLine.Args.Url] = serverUrl;
        }
    }
}
