using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IExtension : IAgentService
    {
        Type ExtensionType { get; }
    }

    public interface ICommandExtension : IExtension
    {
        string CommandArea { get; }
    }

    public interface IVariablesExtension : IExtension
    {
        string Get();
        string Set();
    }
}