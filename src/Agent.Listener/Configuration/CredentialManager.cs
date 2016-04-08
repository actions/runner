using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(CredentialManager))]
    public interface ICredentialManager: IAgentService
    {
        ICredentialProvider GetCredentialProvider(string credType);
        VssCredentials LoadCredentials();
    }

    public class CredentialManager : AgentService, ICredentialManager
    {        
        public static readonly Dictionary<string, Type> CredentialTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "PAT", typeof(PersonalAccessToken)},
            { "ALT", typeof(AlternateCredential)},
            { "NTLM", typeof(NTLMCredential)}
        };

        public ICredentialProvider GetCredentialProvider(string credType)
        {
            Trace.Info("Create()");
            Trace.Info("Creating type {0}", credType);

            if (!CredentialTypes.ContainsKey(credType))
            {
                throw new ArgumentException("Invalid Credential Type");
            }

            Trace.Info("Creating credential type: {0}", credType);
            var creds = Activator.CreateInstance(CredentialTypes[credType]) as ICredentialProvider;
            Trace.Verbose("Created credential type");
            return creds;
        }
        
        public VssCredentials LoadCredentials()
        {
            IConfigurationStore store = HostContext.GetService<IConfigurationStore>(); 

            if (!store.HasCredentials())
            {
                throw new InvalidOperationException("Credentials not stored.  Must reconfigure.");
            }
                        
            CredentialData credData = store.GetCredentials();
            ICredentialProvider credProv = GetCredentialProvider(credData.Scheme);
            credProv.CredentialData = credData;
            
            VssCredentials creds = credProv.GetVssCredentials(HostContext);

            return creds;
        }
    }
}