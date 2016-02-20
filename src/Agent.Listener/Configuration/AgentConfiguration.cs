namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    public sealed class AgentConfiguration
    {
        public IAgentSettings Setting { get; set; }

        public AgentCredential Credential { get; set; }
    }
}