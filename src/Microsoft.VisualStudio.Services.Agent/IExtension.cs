using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IExtension
    {
        Type ExtensionType { get; }
    }
    
    public interface ICommandExtension : IExtension
    {
        String CommandArea { get; }
    }

    public interface IJobExtension : IExtension
    {        
        String HostTypes { get; }
    }
}