using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Agent.Worker.Release
{
    public class ReleaseServer
    {
        private Uri _projectCollectionUrl;
        private VssCredentials _credential;
        private Guid _projectId;

        private ReleaseHttpClient _releaseHttpClient { get; }

        public ReleaseServer(Uri projectCollection, VssCredentials credentials, Guid projectId)
        {
            ArgUtil.NotNull(projectCollection, nameof(projectCollection));
            ArgUtil.NotNull(credentials, nameof(credentials));

            _projectCollectionUrl = projectCollection;
            _credential = credentials;
            _projectId = projectId;

            _releaseHttpClient = new ReleaseHttpClient(projectCollection, credentials, new VssHttpRetryMessageHandler(3));
        }

        public IEnumerable<AgentArtifactDefinition> GetReleaseArtifactsFromService(int releaseId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var artifacts = _releaseHttpClient.GetAgentArtifactDefinitionsAsync(_projectId, releaseId, cancellationToken: cancellationToken).Result;
            return artifacts;
        }
    }
}