using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    [ServiceLocator(Default = typeof(CredentialManager))]
    public interface ICredentialManager: IAgentService
    {
        ICredentialProvider GetCredentialProvider(string credType);
    }

    public class CredentialManager : AgentService, ICredentialManager
    {
        public static readonly Dictionary<string, Type> CredentialTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "PAT", typeof(PersonalAccessToken)},
            { "ALT", typeof(AlternateCredential)}
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
            return Activator.CreateInstance(CredentialTypes[credType]) as ICredentialProvider;
            Trace.Verbose("Created credential type");
        }
    }
}