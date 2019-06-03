using System;

namespace Runner.Common
{
    public interface IExtension : IAgentService
    {
        Type ExtensionType { get; }
    }
}
