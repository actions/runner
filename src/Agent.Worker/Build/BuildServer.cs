using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Build2 = Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class BuildServer
    {
        private readonly Build2.BuildHttpClient _buildHttpClient;
        private Guid _projectId;

        public BuildServer(VssConnection connection, Guid projectId)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            _projectId = projectId;
            _buildHttpClient = connection.GetClient<Build2.BuildHttpClient>();
        }

        public async Task<Build2.BuildArtifact> AssociateArtifact(
            int buildId,
            string name,
            string type,
            string data,
            Dictionary<string, string> propertiesDictionary,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Build2.BuildArtifact artifact = new Build2.BuildArtifact()
            {
                Name = name,
                Resource = new Build2.ArtifactResource()
                {
                    Data = data,
                    Type = type,
                    Properties = propertiesDictionary
                }
            };

            return await _buildHttpClient.CreateArtifactAsync(artifact, _projectId, buildId, cancellationToken: cancellationToken);
        }

        // public async Task<Build2.BuildArtifact> DownloadBuildArtifact(
        //     string project, 
        //     int buildId, 
        //     string artifactName, 
        //     CancellationToken cancellationToken = default(CancellationToken))
        // {
        //     return await _buildHttpClient.GetArtifactAsync(project, buildId, artifactName, null, cancellationToken);
        // }

        public async Task<Build2.Build> UpdateBuildNumber(
            int buildId,
            string buildNumber,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Build2.Build build = new Build2.Build()
            {
                Id = buildId,
                BuildNumber = buildNumber,
                Project = new TeamProjectReference()
                {
                    Id = _projectId,
                },
            };

            return await _buildHttpClient.UpdateBuildAsync(build, _projectId, buildId, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<string>> AddBuildTag(
            int buildId,
            string buildTag,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _buildHttpClient.AddBuildTagAsync(_projectId, buildId, buildTag, cancellationToken: cancellationToken);
        }
    }
}
