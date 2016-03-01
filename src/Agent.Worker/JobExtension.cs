using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        string HostType { get; }
        IStep PrepareStep { get; }
        IStep FinallyStep { get; }
    }
}