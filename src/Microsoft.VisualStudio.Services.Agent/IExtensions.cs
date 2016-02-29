using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IExtension
    {
        Type ExtensionType { get; }
    }
    
    public interface ICommandExtension : IExtension
    {
        string CommandArea { get; }
    }

    public interface IJobExtension : IExtension
    {        
        string HostTypes { get; }
    }
    
    public interface IVariablesExtension : IExtension
    {        
        string Get();
        string Set();
    }    
}