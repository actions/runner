using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.VisualStudio.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(BuildArtifactProvider))]
    public interface IBuildArtifactProvider : IAgentService
    {
        Task Download(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string downloadFolderPath);
    }

    public class BuildArtifactProvider : AgentService, IBuildArtifactProvider
    {
        private readonly Dictionary<ArtifactType, IArtifact> artifactMap = new Dictionary<ArtifactType, IArtifact>
        {
            { ArtifactType.Build, new BuildArtifact() },
            { ArtifactType.Jenkins, new JenkinsArtifact() }
        };

        public async Task Download(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string workingFolder)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNullOrEmpty(workingFolder, nameof(workingFolder));

            try
            {
                await artifactMap[artifactDefinition.ArtifactType].Download(artifactDefinition, HostContext, executionContext, workingFolder);
            }
            catch (Exception exception)
            {
                Trace.Error(exception);
                throw;
            }
        }
    }
}