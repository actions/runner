using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IExtension : IAgentService
    {
        Type ExtensionType { get; }
    }
}