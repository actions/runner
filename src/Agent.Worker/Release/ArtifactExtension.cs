using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public interface IArtifactExtension : IExtension
    {
        AgentArtifactType ArtifactType { get; }

        Task DownloadAsync(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string downloadFolderPath);

        IArtifactDetails GetArtifactDetails(IExecutionContext context, AgentArtifactDefinition agentArtifactDefinition);
    }
}