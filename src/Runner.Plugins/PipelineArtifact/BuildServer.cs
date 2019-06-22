using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.Services.WebApi;
using GitHub.Build.WebApi;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    // A client wrapper interacting with TFS/Build's Artifact API
    public class BuildServer
    {
        private readonly BuildHttpClient _buildHttpClient;

        public BuildServer(VssConnection connection)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            _buildHttpClient = connection.GetClient<BuildHttpClient>();
        }

        // Associate the specified artifact with a build, along with custom data.
        public async Task<BuildArtifact> AssociateArtifact(
            Guid projectId,
            int pipelineId,
            string name,
            string type,
            string data,
            Dictionary<string, string> propertiesDictionary,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BuildArtifact artifact = new BuildArtifact()
            {
                Name = name,
                Resource = new ArtifactResource()
                {
                    Data = data,
                    Type = type,
                    Properties = propertiesDictionary
                }
            };

            return await _buildHttpClient.CreateArtifactAsync(artifact, projectId, pipelineId, cancellationToken: cancellationToken);
        }

        // Get named artifact from a build
        public async Task<BuildArtifact> GetArtifact(
            Guid projectId,
            int pipelineId,
            string name,
            CancellationToken cancellationToken)
        {
            return await _buildHttpClient.GetArtifactAsync(projectId, pipelineId, name, cancellationToken: cancellationToken);
        }

        public Task<List<BuildArtifact>> GetArtifactsAsync(
            Guid project,
            int pipelineId,
            CancellationToken cancellationToken)
        {
            return _buildHttpClient.GetArtifactsAsync(project, pipelineId, userState: null, cancellationToken: cancellationToken);
        }

        //Get artifact with project name.
        public async Task<BuildArtifact> GetArtifactWithProjectNameAsync(
            string project,
            int pipelineId,
            string name,
            CancellationToken cancellationToken)
        {
            return await _buildHttpClient.GetArtifactAsync(project, pipelineId, name, cancellationToken: cancellationToken);
        }

        public Task<List<BuildArtifact>> GetArtifactsWithProjectNameAsync(
            string project,
            int pipelineId,
            CancellationToken cancellationToken)
        {
            return _buildHttpClient.GetArtifactsAsync(project, pipelineId, userState: null, cancellationToken: cancellationToken);
        }
    }
}
