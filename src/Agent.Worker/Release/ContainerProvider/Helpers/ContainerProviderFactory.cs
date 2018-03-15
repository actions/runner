using System;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerProvider.Helpers
{
    public class ContainerProviderFactory
    {
        private readonly BuildArtifactDetails _buildArtifactDetails;
        private readonly string _rootLocation;
        private readonly int _containerId;
        private readonly IExecutionContext _executionContext;
        private readonly HttpRetryOnTimeoutMessageHandler _retryOnTimeoutMessageHandler;

        public ContainerProviderFactory(BuildArtifactDetails buildArtifactDetails, string rootLocation, int containerId, IExecutionContext executionContext)
        {
            this._buildArtifactDetails = buildArtifactDetails;
            this._rootLocation = rootLocation;
            this._containerId = containerId;
            this._executionContext = executionContext;

            var executionLogger = new ExecutionLogger(executionContext);

            var httpRetryOnTimeoutOptions = new HttpRetryOnTimeoutOptions
            {
                MaxRetries = 5,
                MinBackoff = TimeSpan.FromSeconds(30),
                BackoffCoefficient = TimeSpan.FromSeconds(10),
            };

            _retryOnTimeoutMessageHandler = new HttpRetryOnTimeoutMessageHandler(
                httpRetryOnTimeoutOptions,
                executionLogger);
        }

        public IContainerProvider GetContainerProvider(string containerType)
        {
            switch (containerType)
            {
                case ArtifactResourceTypes.Container:

                    var fileContainerItemCache = new FileContainerProvider(
                        this._containerId,
                        this._buildArtifactDetails.Project,
                        this._rootLocation,
                        this._buildArtifactDetails.TfsUrl,
                        this._buildArtifactDetails.AccessToken,
                        this._retryOnTimeoutMessageHandler,
                        this._executionContext,
                        includeDownloadTickets: true);

                    return fileContainerItemCache;

                default:
                    throw new ArtifactDownloadException((StringUtil.Loc("RMArtifactTypeNotSupported", containerType)));
            }
        }
    }
}