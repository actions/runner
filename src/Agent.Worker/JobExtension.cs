namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        HostTypes HostType { get; }
        IStep PreJobStep { get; }
        IStep ExecutionStep { get; }
        IStep PostJobStep { get; }
        string GetRootedPath(IExecutionContext context, string path);
        void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);
    }
}