namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ReleaseDirectoryManager))]
    public interface IReleaseDirectoryManager : IAgentService
    {
        ReleaseDefinitionToFolderMap PrepareArtifactsDirectory(
            string workingDirectory,
            string collectionId,
            string projectId,
            string releaseDefinition);
    }
}