using Microsoft.TeamFoundation.Core.WebApi;
using Agent.Sdk;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Agent.Plugins.Drop
{
    public class BuildServer
    {
        private readonly BuildHttpClient _buildHttpClient;

        public BuildServer(VssConnection connection)
        {
            PluginUtil.NotNull(connection, nameof(connection));
            _buildHttpClient = connection.GetClient<BuildHttpClient>();
        }

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
    }
}
