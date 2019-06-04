using System;

namespace GitHub.Runner.Common
{
    public interface IExtension : IAgentService
    {
        Type ExtensionType { get; }
    }
}
