using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{

    [ServiceLocator(Default = typeof(AgentCredentialManager))]
    public interface IAgentCredentialManager
    {
        AgentCredential Create(AuthScheme authScheme);
    }

    public class AgentCredentialManager : IAgentCredentialManager
    {
        private static Dictionary<AuthScheme, Func<AgentCredential>> credentialFactory =
            new Dictionary<AuthScheme, Func<AgentCredential>> { { AuthScheme.Pat, () => new TokenCredential() } };

        public AgentCredential Create(AuthScheme authScheme)
        {
            return credentialFactory[authScheme]();
        }
    }
}