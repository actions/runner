using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.Pipelines.WebApi;
using GitHub.Services.WebApi;
using GitHub.Runner.Sdk;
using Pipelines = GitHub.Actions.Pipelines.WebApi;

namespace GitHub.Runner.Plugins.Artifact
{
    // A client wrapper interacting with Pipelines's Artifact API
    public class PipelinesServer
    {
        private readonly PipelinesHttpClient _pipelinesHttpClient;

        public PipelinesServer(VssConnection connection)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            _pipelinesHttpClient = connection.GetClient<PipelinesHttpClient>();
        }

        // Associate the specified Actions Storage artifact with a pipeline
        public async Task<Pipelines.ActionsStorageArtifact> AssociateActionsStorageArtifactAsync(
            int pipelineId,
            int runId,
            long containerId,
            string name,
            long size,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CreateArtifactParameters parameters = new CreateActionsStorageArtifactParameters()
            {
                Name = name,
                ContainerId = containerId,
                Size = size
            };

            return await _pipelinesHttpClient.CreateArtifactAsync(
                parameters,
                pipelineId, 
                runId,
                cancellationToken: cancellationToken) as Pipelines.ActionsStorageArtifact;
        }

        // Get named Actions Storage artifact for a pipeline
        public async Task<Pipelines.ActionsStorageArtifact> GetActionsStorageArtifact(
            int pipelineId,
            int runId,
            string name,
            CancellationToken cancellationToken)
        {
            return await _pipelinesHttpClient.GetArtifactAsync(
                pipelineId,
                runId,
                name,
                cancellationToken: cancellationToken) as Pipelines.ActionsStorageArtifact;
        }
    }
}
