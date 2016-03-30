using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        string HostType { get; }
        IStep PrepareStep { get; }
        IStep FinallyStep { get; }
        void GetRootedPath(IExecutionContext context, string path, out string rootedPath);
        void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);
    }
}