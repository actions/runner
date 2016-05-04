using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    [ServiceLocator(Default = typeof(CodeCoverageServer))]
    public interface ICodeCoverageServer : IAgentService
    {
        /// <summary>
        /// Publish Artifact to build
        /// </summary>
        Task CreateArtifactAsync(IAsyncCommandContext context, VssConnection connection, Guid projectId, int buildId, long containerId, string name, string fileContainerPath, bool browsable, CancellationToken cancellationToken);

        /// <summary>
        /// Publish code coverage summary
        /// </summary>
        Task PublishCoverageSummaryAsync(VssConnection connection, string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken);
    }

    internal class CodeCoverageServer : AgentService, ICodeCoverageServer
    {
        public async Task CreateArtifactAsync(IAsyncCommandContext context, VssConnection connection, Guid projectId, int buildId, long containerId, string name, string source, bool browsable, CancellationToken cancellationToken)
        {
            var browsableProperty = (browsable) ? bool.TrueString : bool.FalseString;
            var artifactProperties = new Dictionary<string, string> {
                    { ArtifactUploadEventProperties.ContainerFolder, name},
                    { ArtifactUploadEventProperties.ArtifactName, name },
                    { ArtifactAssociateEventProperties.ArtifactType, WellKnownArtifactResourceTypes.Container },
                    { ArtifactAssociateEventProperties.Browsable, browsableProperty },
                };

            FileContainerServer fileContainerHelper = new FileContainerServer(connection.Uri, connection.Credentials, projectId, containerId, name);
            await fileContainerHelper.CopyToContainerAsync(context, source, cancellationToken);
            string fileContainerFullPath = StringUtil.Format($"#/{containerId}/{name}");

            Build.BuildServer buildHelper = new Build.BuildServer(connection.Uri, connection.Credentials, projectId);
            var artifact = await buildHelper.AssociateArtifact(buildId, name, WellKnownArtifactResourceTypes.Container, fileContainerFullPath, artifactProperties, cancellationToken);
            context.Output(StringUtil.Loc("PublishedCodeCoverageArtifact", source, name));
        }

        public async Task PublishCoverageSummaryAsync(VssConnection connection, string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken)
        {
            var testHttpClient = connection.GetClient<TestManagementHttpClient>();
            // <todo: Bug 402783> We are currently passing BuildFlavor and BuildPlatform = "" There value are required be passed to command
            await testHttpClient.UpdateCodeCoverageSummaryAsync(project, buildId,
               new CodeCoverageData() { BuildFlavor = "", BuildPlatform = "", CoverageStats = coverageData.ToList() },
               cancellationToken: cancellationToken);
        }
    }
}
