using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.VisualStudio.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ArtifactProvider))]
    public interface IArtifactProvider : IAgentService
    {
        Task Download(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string downloadFolderPath);

        IArtifactDetails GetArtifactDetails(IExecutionContext context, AgentArtifactDefinition agentArtifactDefinition);
    }

    public class ArtifactProvider : AgentService, IArtifactProvider
    {
        public async Task Download(IExecutionContext executionContext, ArtifactDefinition artifactDefinition, string workingFolder)
        {
            Trace.Entering();

            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNullOrEmpty(workingFolder, nameof(workingFolder));

            try
            {
                // TODO: Avoid this if-else case by implementing Custom ServiceLocator attribute
                if (artifactDefinition.ArtifactType == ArtifactType.Build)
                {
                    await 
                        HostContext.GetService<IBuildArtifact>()
                            .Download(artifactDefinition, executionContext, workingFolder);
                }
                else if (artifactDefinition.ArtifactType == ArtifactType.Jenkins)
                {
                    await
                        HostContext.GetService<IJenkinsArtifact>()
                            .Download(artifactDefinition, executionContext, workingFolder);
                }

            }
            catch (Exception exception)
            {
                Trace.Error(exception);
                throw;
            }
        }

        public IArtifactDetails GetArtifactDetails(IExecutionContext executionContext, AgentArtifactDefinition agentArtifactDefinition)
        {
            Trace.Entering();

            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(agentArtifactDefinition, nameof(agentArtifactDefinition));

            // TODO: Check if we can merge ArtifactType and AgentArtifactType
            if (agentArtifactDefinition.ArtifactType == AgentArtifactType.Build)
            {
                return HostContext.GetService<IBuildArtifact>()
                    .GetArtifactDetails(agentArtifactDefinition, executionContext);
            }
            else if (agentArtifactDefinition.ArtifactType == AgentArtifactType.Jenkins)
            {
                return HostContext.GetService<IJenkinsArtifact>()
                    .GetArtifactDetails(agentArtifactDefinition, executionContext);
            }

            throw new InvalidOperationException(StringUtil.Loc("RMArtifactTypeNotSupported"));
        }
    }
}