using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Agent.Sdk;

namespace Agent.Plugins.PipelineArtifact
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
            int buildId,
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

            return await _buildHttpClient.CreateArtifactAsync(artifact, projectId, buildId, cancellationToken: cancellationToken);
        }

        // Get named artifact from a build
        public async Task<BuildArtifact> GetArtifact(
            Guid projectId,
            int buildId,
            string name,
            CancellationToken cancellationToken)
        {
            return await _buildHttpClient.GetArtifactAsync(projectId, buildId, name, cancellationToken: cancellationToken);
        }

        public Task<List<BuildArtifact>> GetArtifactsAsync(
            Guid project,
            int buildId,
            CancellationToken cancellationToken)
        {
            return _buildHttpClient.GetArtifactsAsync(project, buildId, userState: null, cancellationToken: cancellationToken);
        }

        //Get artifact with project name.
        public async Task<BuildArtifact> GetArtifactWithProjectNameAsync(
            string project,
            int buildId,
            string name,
            CancellationToken cancellationToken)
        {
            return await _buildHttpClient.GetArtifactAsync(project, buildId, name, cancellationToken: cancellationToken);
        }

        public Task<List<BuildArtifact>> GetArtifactsWithProjectNameAsync(
            string project,
            int buildId,
            CancellationToken cancellationToken)
        {
            return _buildHttpClient.GetArtifactsAsync(project, buildId, userState: null, cancellationToken: cancellationToken);
        }
    }
}
