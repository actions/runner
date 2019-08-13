using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.Services.WebApi;
using GitHub.Build.WebApi;

namespace GitHub.Runner.Plugins.Artifact
{
    // A client wrapper interacting with Build's Artifact API
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
            string jobId,
            string name,
            string type,
            string data,
            Dictionary<string, string> propertiesDictionary,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BuildArtifact artifact = new BuildArtifact()
            {
                Name = name,
                Source = jobId,
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
    }
}
