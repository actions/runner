using Microsoft.VisualStudio.Services.Agent;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        string HostType { get; }
        IStep PrepareStep { get; }
        IStep FinallyStep { get; }
        string GetRootedPath(IExecutionContext context, string path);
        void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);
    }
}