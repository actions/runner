using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    // TODO: Refactor extension manager to enable using it from the agent process.
    [ServiceLocator(Default = typeof(CredentialManager))]
    public interface ICredentialManager : IAgentService
    {
        ICredentialProvider GetCredentialProvider(string credType);
        VssCredentials LoadCredentials();
    }

    public class CredentialManager : AgentService, ICredentialManager
    {        
        public static readonly Dictionary<string, Type> CredentialTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { Constants.Configuration.AAD, typeof(AadDeviceCodeAccessToken)},
            { Constants.Configuration.PAT, typeof(PersonalAccessToken)},
            { Constants.Configuration.Alternate, typeof(AlternateCredential)},
            { Constants.Configuration.Negotiate, typeof(NegotiateCredential)},
            { Constants.Configuration.Integrated, typeof(IntegratedCredential)},
            { Constants.Configuration.OAuth, typeof(OAuthCredential)},
            { Constants.Configuration.ServiceIdentity, typeof(ServiceIdentityCredential)},
        };

        public ICredentialProvider GetCredentialProvider(string credType)
        {
            Trace.Info(nameof(GetCredentialProvider));
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